'use strict';
const os=require('os');
function recommend({memoryGB,availableGB,threads}){const budget=Math.min(memoryGB*.65,availableGB);return {whisper:budget>=8&&threads>=12?'small':budget>=4&&threads>=4?'base':'tiny',deepseek:budget>=10?'Q4_K_M':budget>=6?'IQ3_M':budget>=4?'IQ2_M':null}}
function inspect(){const cpus=os.cpus();const hardware={cpu:cpus[0]?.model||'未知',threads:cpus.length,memoryGB:os.totalmem()/1073741824,availableGB:os.freemem()/1073741824,transcriptionDevice:'cpu'};return {checkedAt:new Date().toISOString(),hardware,recommended:recommend(hardware)}}
module.exports={inspect,recommend};
