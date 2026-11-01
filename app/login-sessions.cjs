const {createReader}=require('./image-browser.cjs');const desktop=require('./desktop-bridge.cjs');
const sessions=new Map(),opening=new Set();
const scope=task=>task.mode==='search'?task.sources:[task.url];
const signature=task=>JSON.stringify(scope(task));
function entry(task){let url=scope(task)[0].replaceAll('{{query}}',encodeURIComponent(task.prompt||''));const u=new URL(url);if(['huaban.com','www.huaban.com'].includes(u.hostname)){u.pathname='/';u.search='';}return u.href;}
async function open(task){if(opening.has(task.id))throw Error('采集浏览器正在打开');opening.add(task.id);try{let session=sessions.get(task.id);if(session?.busy)throw Error('采集正在运行');if(session&&(!session.reader.alive()||session.signature!==signature(task))){await session.reader.close().catch(()=>{});sessions.delete(task.id);session=null;}if(!session){const reader=desktop.active()?desktop.reader(scope(task)):await createReader(scope(task),{visible:true});session={reader,signature:signature(task),ready:false,busy:false};sessions.set(task.id,session);}await session.reader.openLogin(entry(task));return {status:'等待你登录或完成验证'};}finally{opening.delete(task.id);}}
function ready(task){const session=sessions.get(task.id);if(!session||!session.reader.alive())throw Error('请先打开采集浏览器并登录');if(session.signature!==signature(task))throw Error('网站范围已修改，请重新打开采集浏览器');if(session.busy)throw Error('此会话正在采集');session.ready=true;}
function borrow(task){if(desktop.active())return desktop.reader(scope(task));const session=sessions.get(task.id);if(!session?.ready)return null;if(!session.reader.alive())throw Error('采集浏览器已关闭，请重新登录');if(session.signature!==signature(task))throw Error('网站范围已修改，请重新登录');if(session.busy)throw Error('浏览器会话正在使用');session.busy=true;return {...session.reader,close:async()=>{session.busy=false;}};}
async function close(id){const session=sessions.get(id);if(session?.busy)throw Error('请等待采集结束后关闭');sessions.delete(id);await session?.reader.close().catch(()=>{});}
function status(){return [...sessions].filter(([,s])=>s.reader.alive()).map(([taskId,s])=>({taskId,status:s.busy?'同会话采集中':s.ready?'登录会话可用':'等待登录确认'}));}
module.exports={open,ready,borrow,close,status};
