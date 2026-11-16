const http=require('http'),fs=require('fs'),fsp=fs.promises,path=require('path'),os=require('os'),{spawn}=require('child_process');
const PORT=Number(process.env.PORT)||3817;
function endpoint(value,suffix){let u;try{u=new URL(value)}catch{throw Error('接口地址无效：请填写以 https:// 开头的完整地址；本地转写请点击“使用本地 Whisper”')}if(u.username||u.password||u.search||u.hash||!u.pathname.endsWith(suffix)||!(u.protocol==='https:'||(u.protocol==='http:'&&['localhost','127.0.0.1','[::1]'].includes(u.hostname))))throw Error('接口地址无效');return u;}
async function read(req,max){let n=0,parts=[];for await(const c of req){n+=c.length;if(n>max)throw Error('请求超过大小限制');parts.push(c)}return Buffer.concat(parts)}
async function jsonResponse(response){if(!response.ok)throw Error('服务返回 HTTP '+response.status+'，请检查接口、模型、密钥和额度');return JSON.parse((await read(response.body,Infinity)).toString())}
function convert(input,output,signal,onProgress=()=>{}){return new Promise((resolve,reject)=>{const child=spawn(process.env.GENTLYPICK_FFMPEG||path.join(__dirname,'..','tools','ffmpeg.exe'),['-hide_banner','-nostdin','-y','-i',input,'-map','0:a:0','-vn','-ac','1','-ar','16000','-c:a','libmp3lame','-b:a','48k','-progress','pipe:1',output],{windowsHide:true});let progress='';child.stdout.on('data',b=>{progress=(progress+b).slice(-10000);const times=[...progress.matchAll(/out_time_us=(\d+)/g)];if(times.length)onProgress({stage:'extracting',seconds:Number(times.at(-1)[1])/1000000})});child.stderr.resume();const abort=()=>child.kill();signal.addEventListener('abort',abort);if(signal.aborted)abort();child.on('error',()=>{signal.removeEventListener('abort',abort);reject(Error('音频提取工具无法启动'))});child.on('close',code=>{signal.removeEventListener('abort',abort);if(signal.aborted)return reject(Error('已取消或处理超时'));if(code!==0)return reject(Error('无法提取音轨：视频可能没有声音或格式不支持'));resolve()})})}
function localTranscribe(audio,model,signal,onProgress=()=>{}){return new Promise(async(resolve,reject)=>{let dir;try{dir=await fsp.mkdtemp(path.join(os.tmpdir(),'gentlypick-whisper-'));const file=path.join(dir,'audio.mp3');await fsp.writeFile(file,Buffer.from(audio,'base64'));const python=process.env.GENTLYPICK_PYTHON||'python';const child=spawn(python,[path.join(__dirname,'transcribe.py'),file,model],{windowsHide:true,env:{...process.env,PYTHONUTF8:'1',PYTHONIOENCODING:'utf-8'}});let out='',err='';child.stdout.on('data',x=>out+=x.toString());let pending='';const decoder=new (require('string_decoder').StringDecoder)('utf8');child.stderr.on('data',x=>{const chunk=decoder.write(x);err=(err+chunk).slice(-4000);pending+=chunk;let end;while((end=pending.indexOf('\n'))>=0){const line=pending.slice(0,end);pending=pending.slice(end+1);if(line.startsWith('GENTLYPICK_PROGRESS ')){try{onProgress(JSON.parse(line.slice(20)))}catch{}}}});const abort=()=>child.kill();signal.addEventListener('abort',abort);if(signal.aborted)abort();child.on('error',e=>reject(Error('本地 Python 无法启动：'+e.message)));child.on('close',async code=>{signal.removeEventListener('abort',abort);await fsp.rm(dir,{recursive:true,force:true}).catch(()=>{});if(signal.aborted)return reject(Error('本地转写已取消'));if(code!==0){const detail=err.trim();return reject(Error(detail?'本地转写失败：'+detail:'本地转写进程异常退出（退出码 '+(code===null?'未知':code)+'）。请在本地模型中安装或检查 Whisper 运行环境。'))};try{resolve(JSON.parse(out))}catch{reject(Error('本地转写返回内容无效'))}})}catch(e){if(dir)await fsp.rm(dir,{recursive:true,force:true}).catch(()=>{});reject(e)}})}
const server=http.createServer(async(req,res)=>{
 const host=req.headers.host,send=(code,data)=>{if(!res.destroyed){res.writeHead(code,{'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store'});res.end(JSON.stringify(data))}};
 if(!/^(localhost|127\.0\.0\.1)(:\d+)?$/.test(host||''))return send(403,{error:'本机访问限制'});
 if(req.method==='GET'&&req.url==='/api/status')return send(200,{localWhisper:true});
 if(req.method==='GET'&&req.url==='/api/local-models'){try{const response=await fetch('http://127.0.0.1:11434/api/tags',{signal:AbortSignal.timeout(3000)});if(!response.ok)throw Error();const data=await response.json();return send(200,{models:(data.models||[]).map(x=>x.name).filter(x=>typeof x==='string')})}catch{return send(503,{error:'本地模型服务未启动，请先启动 DeepSeek'})}}
 if(req.method==='GET'&&req.url==='/logo.png'){res.writeHead(200,{'Content-Type':'image/png','Cache-Control':'no-cache'});return fs.createReadStream(path.join(__dirname,'logo.png')).pipe(res)}
 const routes={'/':'index.html','/app.js':'app.js'};
 if(req.method==='GET'&&routes[req.url]){res.writeHead(200,{'Content-Type':req.url.endsWith('.js')?'text/javascript; charset=utf-8':'text/html; charset=utf-8','Cache-Control':'no-store'});return res.end(fs.readFileSync(path.join(__dirname,routes[req.url])))}
 if(req.method!=='POST'||!['/api/audio','/api/transcribe','/api/analyze'].includes(req.url))return send(404,{error:'不存在'});
 if(req.headers.origin!==`http://${host}`)return send(403,{error:'拒绝外部页面请求'});
 const controller=new AbortController();res.on('close',()=>controller.abort());let dir;
 try{
  if(req.url==='/api/audio'){

   dir=await fsp.mkdtemp(path.join(os.tmpdir(),'gentlypick-audio-'));const input=path.join(dir,'input.video'),output=path.join(dir,'audio.mp3');const handle=await fsp.open(input,'w');let size=0;
   try{for await(const chunk of req){size+=chunk.length;await handle.writeFile(chunk)}}finally{await handle.close()}
   res.writeHead(200,{'Content-Type':'application/x-ndjson; charset=utf-8','Cache-Control':'no-store'});res.flushHeaders();const emit=data=>{if(!res.destroyed)res.write(JSON.stringify(data)+'\n')};try{await convert(input,output,controller.signal,p=>emit({type:'progress',...p}));const audio=await fsp.readFile(output);emit({type:'result',audio:audio.toString('base64'),mime:'audio/mpeg'})}catch(e){emit({type:'error',error:e.message})}finally{res.end()}
  }else{
   const data=JSON.parse((await read(req,req.url==='/api/transcribe'?Infinity:18*1024*1024)).toString());if(typeof data.key!=='string'||data.key.length>4000)throw Error('密钥格式无效');const headers=data.key?{Authorization:'Bearer '+data.key}:{};let body,url;
   if(req.url==='/api/transcribe'){
    if(typeof data.model!=='string'||!data.model.trim()||typeof data.audio!=='string')throw Error('转写参数不完整');
    if(data.endpoint==='local-whisper'){res.writeHead(200,{'Content-Type':'application/x-ndjson; charset=utf-8','Cache-Control':'no-store'});res.flushHeaders();const emit=data=>{if(!res.destroyed)res.write(JSON.stringify(data)+'\n')};const heartbeat=setInterval(()=>emit({type:'heartbeat'}),1000);try{const result=await localTranscribe(data.audio,data.model,controller.signal,p=>emit({type:'progress',...p}));emit({type:'result',...result})}catch(e){emit({type:'error',error:e.message})}finally{clearInterval(heartbeat);res.end()}return}
    url=endpoint(data.endpoint,'/audio/transcriptions');
    body=new FormData();body.append('file',new Blob([Buffer.from(data.audio,'base64')],{type:'audio/mpeg'}),'audio.mp3');body.append('model',data.model);body.append('response_format','verbose_json');body.append('timestamp_granularities[]','segment');
   }else{url=endpoint(data.endpoint,'/chat/completions');if(!Array.isArray(data.body?.messages)||typeof data.body.model!=='string')throw Error('分析参数不完整');body=JSON.stringify(data.body);headers['Content-Type']='application/json'}
   send(200,await jsonResponse(await fetch(url,{method:'POST',redirect:'error',signal:controller.signal,headers,body})));
  }
 }catch(e){send(502,{error:controller.signal.aborted?'操作已取消':e.message.startsWith('Unexpected')?'服务返回了无效 JSON':e.message})}
 finally{if(dir)await fsp.rm(dir,{recursive:true,force:true}).catch(()=>{})}
});
server.requestTimeout=0;server.timeout=0;
module.exports=server;
