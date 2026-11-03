const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),http=require('node:http'),net=require('node:net'),os=require('node:os'),path=require('node:path'),{spawn}=require('node:child_process');
const APP=fs.existsSync(path.join(__dirname,'extraction.cjs'))?__dirname:path.join(__dirname,'..','app');
const {extract}=require(path.join(APP,'extraction.cjs'));
const sample=`<!doctype html><title>测试文章</title><article><p>正文开始</p><table><tr><th>型号</th><th>价格</th></tr><tr><td rowspan="2">A</td><td>&lt;script&gt;bad&lt;/script&gt;</td></tr><tr><td>200</td></tr></table><pre><code class="language-js">function test() {\n  return &quot;x&quot;;\n}</code></pre><p>正文结束</p><div class="comment">不要收录的评论</div></article>`;
const result=extract(sample,'http://127.0.0.1/');
assert.equal(result.blocks.filter(x=>x.type==='table').length,1);assert.equal(result.blocks.filter(x=>x.type==='code').length,1);
assert.match(result.text,/型号\t价格/);assert.match(result.text,/\n  return "x";/);assert.ok(!result.text.includes('不要收录'));
assert.equal(result.blocks.find(x=>x.type==='table').rows[1][0].rowspan,2);
const multi=extract('<article><table><tr><td>第一行<br>第二行</td></tr></table><pre><code><div>if (x) {</div><div>  test();</div><div>}</div></code></pre></article>');
assert.equal(multi.blocks.find(x=>x.type==='table').rows[0][0].text,'第一行\n第二行');assert.equal(multi.blocks.find(x=>x.type==='code').text,'if (x) {\n  test();\n}');
const context={},workspace=fs.readFileSync(path.join(APP,'workspace-ui.js'),'utf8'),boundary=workspace.indexOf('\nlet reportBucket=');assert.ok(boundary>0);vm.runInNewContext(workspace.slice(0,boundary),context);
const report={title:result.title,url:'http://127.0.0.1/',time:'2026-11-02T01:00:00.000Z',text:result.text,blocks:result.blocks,template:'标题：{{title}}\n正文：{{text}}\n来源：{{url}}'};
report.content=report.template.replace(/\{\{(title|time|url|text)\}\}/g,(_,k)=>report[k]);
const html=context.renderFormattedReport(report);assert.match(html,/<table class="excerpt-table">/);assert.match(html,/<pre class="excerpt-code">/);assert.match(html,/rowspan="2"/);assert.ok(!html.includes('<script>bad</script>'));assert.match(html,/&lt;script&gt;bad&lt;\/script&gt;/);
assert.ok(context.renderFormattedReport({...report,blocks:[]}).startsWith('<pre'));assert.ok(context.renderFormattedReport({...report,content:'different'}).startsWith('<pre'));
const twice={...report,template:'{{text}}\n--\n{{text}}',content:report.text+'\n--\n'+report.text};assert.equal((context.renderFormattedReport(twice).match(/<table class="excerpt-table">/g)||[]).length,2);
async function batchTest(){
 const search=require(path.join(APP,'search.cjs')),original=search.discover;search.discover=async()=>({keywords:['测试'],warnings:[],candidates:[{url:'https://example.com/a',title:'测试文章'}]});
 try{
  const state={reports:[]},task={id:'t',name:'测试',mode:'search',sources:['https://example.com/'],prompt:'测试',maxResults:5,template:'{{text}}',depth:{enabled:false},activeRunId:'r'};
  const env={get:async()=>({status:200}),collect:async()=>({...result,url:'https://example.com/a'}),archiveImages:async()=>[],MEDIA:'',state,save:()=>{},render:(template,values)=>template.replace(/\{\{text\}\}/g,values.text)};
  await require(path.join(APP,'batch.cjs')).runBatch(task,env);assert.equal(state.reports.length,1);assert.ok(state.reports[0].blocks.some(x=>x.type==='table'));
  result.blocks.find(x=>x.type==='table').rows[1][0].rowspan=1;
  await require(path.join(APP,'batch.cjs')).runBatch(task,env);assert.equal(state.reports.length,2,'format-only changes must be detected');
 }finally{search.discover=original;}
}
async function integration(){
 const fixture=http.createServer((req,res)=>{res.writeHead(200,{'content-type':'text/html; charset=utf-8'});res.end(sample)});await new Promise(r=>fixture.listen(0,'127.0.0.1',r));
 const reserve=net.createServer();await new Promise(r=>reserve.listen(0,'127.0.0.1',r));const appPort=reserve.address().port;await new Promise(r=>reserve.close(r));
 const tmp=fs.mkdtempSync(path.join(os.tmpdir(),'gentlypick-format-')),dataFile=path.join(tmp,'state.json');
 const child=spawn(process.execPath,['server.cjs'],{cwd:APP,windowsHide:true,env:{...process.env,PORT:String(appPort),EXCERPT_DATA:dataFile,EXCERPT_DESKTOP_TOKEN:'test-token'},stdio:['ignore','pipe','pipe']});
 let diagnostics='';child.stderr.on('data',x=>diagnostics+=x);let started=false;
 try{
  for(let i=0;i<80;i++){try{const r=await fetch(`http://127.0.0.1:${appPort}/api/state`);if(r.ok){started=true;break;}}catch{}await new Promise(r=>setTimeout(r,100));}
  assert.ok(started,'test server failed to start: '+diagnostics);
  const ui=await fetch(`http://127.0.0.1:${appPort}/workspace-ui.js`);assert.equal(ui.status,200);assert.ok((await ui.text()).includes('function renderFormattedReport'));
  const api=async (endpoint,body,headers={})=>{const r=await fetch(`http://127.0.0.1:${appPort}`+endpoint,{method:'POST',headers:{'content-type':'application/json',...headers},body:JSON.stringify(body)});const data=await r.json();assert.ok(r.ok,JSON.stringify(data));return data;};
  const url=`http://127.0.0.1:${fixture.address().port}/page`,template='{{title}}\n{{text}}\n{{url}}';
  const task=await api('/api/tasks',{name:'测试',mode:'url',url,time:'09:00',enabled:false,template});
  const run=await api(`/api/tasks/${task.id}/run`,{});assert.ok(run.report.blocks.some(x=>x.type==='table'));assert.ok(run.report.blocks.some(x=>x.type==='code'));assert.match(run.report.text,/  return "x";/);
  const imported=await api('/api/desktop/import',{taskId:task.id,url,html:sample},{'X-Desktop-Token':'test-token'});assert.equal(imported.title,'测试文章');
  const state=await (await fetch(`http://127.0.0.1:${appPort}/api/state`)).json();assert.ok(state.reports[0].blocks.some(x=>x.type==='table'));
  const rendered=context.renderFormattedReport(state.reports[0]);assert.match(rendered,/<table class="excerpt-table">/);assert.match(rendered,/<pre class="excerpt-code">/);
  const page=fs.readFileSync(path.join(APP,'index.html'),'utf8'),line=page.match(/async function downloadHtml\(id\)\{[^\n]+/);assert.ok(line,'HTML export function missing');let saved;
  const exportContext={data:{reports:[state.reports[0]]},renderFormattedReport:context.renderFormattedReport,esc:s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])),message:()=>{},Blob,URL:{createObjectURL:b=>(saved=b,'blob:test'),revokeObjectURL:()=>{}},document:{createElement:()=>({click(){}})},setTimeout:()=>{}};
  vm.runInNewContext(line[0],exportContext);await exportContext.downloadHtml(state.reports[0].id);const exported=await saved.text();assert.match(exported,/<table class="excerpt-table">/);assert.match(exported,/<pre class="excerpt-code">/);assert.ok(!exported.includes('<script>bad</script>'));
  console.log('PASS extraction, safe rendering, single URL and desktop import');
 }finally{if(child.exitCode===null){child.kill();await new Promise(r=>child.once('exit',r));}await new Promise(r=>fixture.close(r));const base=path.resolve(os.tmpdir())+path.sep;if(!path.resolve(tmp).toLowerCase().startsWith(base.toLowerCase()))throw Error('unsafe test cleanup');fs.rmSync(tmp,{recursive:true,force:true});}
}
batchTest().then(integration).catch(e=>{console.error(e);process.exitCode=1});
