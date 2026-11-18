const {AsyncLocalStorage}=require('node:async_hooks');
const context=new AsyncLocalStorage(),jobs=new Map();
function current(){return context.getStore()}
function check(){const job=current();if(job?.controller.signal.aborted&&!job.finalizing)throw job.controller.signal.reason}
const originalFetch=global.fetch;global.fetch=(url,options={})=>{check();const signal=current()?.controller.signal;return originalFetch(url,signal?{...options,signal:options.signal?AbortSignal.any([signal,options.signal]):signal}:options)};
function run(id,fn){if(jobs.has(id))return Promise.reject(Error('任务正在运行'));const job={controller:new AbortController(),finalizing:false};jobs.set(id,job);return context.run(job,async()=>{try{return await fn()}finally{jobs.delete(id)}})}
function stop(id){const job=jobs.get(id);if(!job)return false;const error=Error('已停止摘录');error.code='TASK_STOPPED';job.controller.abort(error);return true}
module.exports={run,stop,check,current,signal:()=>current()?.controller.signal};
