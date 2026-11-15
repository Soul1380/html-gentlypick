(()=>{
const topWindow=window.top;let bridge;try{bridge=topWindow.chrome?.webview}catch{}
const pending=new Map();
function apply(id,path){const input=pending.get(id);pending.delete(id);if(!input||!path)return;input.value=path;input.dispatchEvent(new Event('input',{bubbles:true}));input.dispatchEvent(new Event('change',{bubbles:true}));}
if(window===topWindow&&bridge){
 window.addEventListener('message',e=>{
  if(e.origin!==location.origin||e.data?.type!=='gentlypick-path')return;
  const frame=document.getElementById('localModelsFrame');
  if(!frame||frame.contentWindow!==e.source||!['folder','file'].includes(e.data.kind)||!/^[\da-f-]{36}$/i.test(e.data.id))return;
  pending.set(e.data.id,e.source);bridge.postMessage(e.data.kind+':'+e.data.id);
 });
 bridge.addEventListener('message',e=>{
  const d=e.data;if(!['folder-result','file-result'].includes(d?.type))return;
  const target=pending.get(d.id);
  if(target?.postMessage){pending.delete(d.id);target.postMessage(d,location.origin)}else apply(d.id,d.path);
 });
}else if(window!==topWindow){
 window.addEventListener('message',e=>{if(e.origin===location.origin&&e.source===window.parent&&['folder-result','file-result'].includes(e.data?.type))apply(e.data.id,e.data.path)});
}
function add(){
 for(const id of ['taskDownloadDirectory','defaultDownloadDirectory','reportDirectory','directory','whisperDirectory','pythonPath']){
 const input=document.getElementById(id);if(!input||input.dataset.pathPicker)continue;input.dataset.pathPicker='1';
 const kind=id==='pythonPath'?'file':'folder',button=document.createElement('button');button.type='button';button.textContent='浏览…';button.className='folder-choose';
 button.setAttribute('aria-label',kind==='file'?'选择 Python 可执行文件':'选择保存文件夹');
 const open=()=>{if(!bridge){alert('请在轻摘桌面版中使用系统路径选择窗口。');return}const request=crypto.randomUUID();pending.set(request,input);if(window===topWindow)bridge.postMessage(kind+':'+request);else window.parent.postMessage({type:'gentlypick-path',id:request,kind},location.origin)};
 button.onclick=open;input.addEventListener('keydown',e=>{if(e.key==='Enter'&&!e.isComposing){e.preventDefault();e.stopPropagation();open()}});input.insertAdjacentElement('afterend',button);
 }
}
add();new MutationObserver(add).observe(document.body,{subtree:true,childList:true});
})();