'use strict';
const fs=require('node:fs'),path=require('node:path'),{Readable,Transform}=require('node:stream'),{pipeline}=require('node:stream/promises'),{execFile,spawn}=require('node:child_process');
function createSetup({runtime,directory}){
 let job=null;
 function start(){if(job&&['downloading','checking','installing','starting'].includes(job.state))return job;job={state:'downloading',downloaded:0,error:''};run().catch(e=>{job.state='failed';job.error=e.message});return {...job};}
 async function run(){if((await runtime.ensure()).available){job.state='ready';return}if(process.platform!=='win32')throw Error('自动安装目前仅支持 Windows');await fs.promises.mkdir(directory,{recursive:true});const file=path.join(directory,'OllamaSetup.exe'),partial=file+'.part';const response=await fetch('https://ollama.com/download/OllamaSetup.exe',{signal:AbortSignal.timeout(30*60*1000)});if(!response.ok||!response.body)throw Error('环境下载失败 HTTP '+response.status);job.total=Number(response.headers.get('content-length'))||null;try{await pipeline(Readable.fromWeb(response.body),new Transform({transform(chunk,enc,cb){job.downloaded+=chunk.length;if(job.downloaded>2*1024**3)return cb(Error('安装文件超过大小限制'));cb(null,chunk)}}),fs.createWriteStream(partial));await fs.promises.rename(partial,file);}catch(e){await fs.promises.unlink(partial).catch(()=>{});throw e}
 job.state='checking';await new Promise((resolve,reject)=>{execFile('powershell.exe',['-NoProfile','-NonInteractive','-Command',"$sig=Get-AuthenticodeSignature -LiteralPath '"+file.replace(/'/g,"''")+"'; if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'Ollama') { exit 1 }"],{windowsHide:true,timeout:60000},e=>e?reject(Error('Ollama 安装包签名验证失败，未运行')):resolve());});
 job.state='installing';await new Promise((resolve,reject)=>{const child=spawn(file,['/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART'],{windowsHide:true,stdio:'ignore'});child.once('error',reject);child.once('exit',code=>code===0?resolve():reject(Error('Ollama 安装未完成，退出码 '+code)));});job.state='starting';const status=await runtime.ensure();if(!status.available)throw Error(status.error||'安装完成，但服务未启动');job.state='ready';
 }
 return {start,snapshot:()=>job&&({...job})};
}
module.exports={createSetup};
