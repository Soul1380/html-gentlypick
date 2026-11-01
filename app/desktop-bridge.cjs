const crypto=require('node:crypto'),{allowed}=require('./search.cjs');
const pending=new Map(),queue=[];let lastPoll=0;
const active=()=>!!process.env.EXCERPT_DESKTOP_TOKEN&&Date.now()-lastPoll<120000;
function request(command){if(!active())return Promise.reject(Error('内置浏览器未就绪，请回到桌面软件'));return new Promise((resolve,reject)=>{const id=crypto.randomUUID(),timer=setTimeout(()=>{pending.delete(id);reject(Error('内置浏览器读取超时，请先登录或完成验证后重试'));},90000);pending.set(id,{resolve,reject,timer});queue.push({...command,id});});}
function poll(){lastPoll=Date.now();let next;while(queue.length){next=queue.shift();if(pending.has(next.id))return next;}return null;}
function complete(input){const entry=pending.get(input.id);if(!entry)return false;pending.delete(input.id);clearTimeout(entry.timer);input.error?entry.reject(Error(String(input.error).slice(0,1000))):entry.resolve(input);return true;}
async function page(url){const r=await request({kind:'read',url,scrollRounds:0});return {url:r.url,text:r.html,status:200,type:'text/html'};}
function reader(scope){return {alive:active,close:async()=>{},openLogin:async url=>request({kind:'open',url}),read:async(url,scrollRounds=0)=>{if(!allowed(url,scope))throw Error('页面超出指定网站范围');const r=await request({kind:'read',url,scrollRounds});if(!allowed(r.url,scope))throw Error('登录后页面跳转超出指定网站范围');return {url:r.url,snapshots:[r.html]};},fetchImage:async(url,options)=>{const r=await request({kind:'image',url,referer:options?.headers?.Referer||''});return new Response(Buffer.from(r.base64,'base64'),{status:200,headers:{'Content-Type':r.contentType}});}};}
module.exports={active,poll,complete,page,reader};
