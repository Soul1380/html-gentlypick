'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),os=require('node:os');
const catalog=require('./local-model-catalog.cjs');
function modelName(item){return item.quant==='Q4_K_M'?'deepseek-r1:8b-llama-distill-q4_K_M':`gentlypick-deepseek:8b-${item.quant.toLowerCase().replace(/_/g,'-')}`;}
function createInventory({runtime,configPath,extraRoots=[]}){
 let config={roots:[],imports:{}};try{config={...config,...JSON.parse(fs.readFileSync(configPath,'utf8'))}}catch{}
 function save(){fs.mkdirSync(path.dirname(configPath),{recursive:true});fs.writeFileSync(configPath,JSON.stringify(config,null,2));}
 function remember(directory){if(typeof directory!=='string'||!path.isAbsolute(directory))throw Error('请选择绝对目录');if(!fs.statSync(directory).isDirectory())throw Error('请选择模型所在文件夹');if(!config.roots.includes(directory)){config.roots.push(directory);save();}}
 async function scan(directory){if(directory)remember(directory);const service=await runtime.status(),files=[],seen=new Set();let count=0;
 const roots=[...config.roots,...extraRoots,process.env.GENTLYPICK_MODEL_HOME,'D:/DeepSeekLocal',path.join(os.homedir(),'Downloads'),path.join(os.homedir(),'.ollama')].filter(Boolean);
 async function walk(dir,depth){let real;try{real=await fs.promises.realpath(dir);if(seen.has(real.toLowerCase()))return;seen.add(real.toLowerCase());const entries=await fs.promises.readdir(dir,{withFileTypes:true});for(const e of entries){if(++count>12000)return;if(e.isSymbolicLink())continue;const full=path.join(dir,e.name);if(e.isDirectory()&&depth<4&&!['node_modules','.git','blobs','vendor'].includes(e.name))await walk(full,depth+1);else if(e.isFile()&&/\.gguf$/i.test(e.name)){const stat=await fs.promises.stat(full);const item=catalog.deepseek.find(m=>m.file.toLowerCase()===e.name.toLowerCase());const known=config.imports[full],name=known||(item?modelName(item):('gentlypick-local:'+crypto.createHash('sha256').update(full).digest('hex').slice(0,12)));files.push({path:full,file:e.name,size:stat.size,id:item?.id||null,name,registered:service.available?service.models.some(m=>m.name===name||m.name===name+':latest'):!!known});}}}catch{}}
 for(const root of roots)await walk(root,0);return {...service,files,roots:config.roots,defaultDirectory:fs.existsSync('D:/DeepSeekLocal')?'D:/DeepSeekLocal/downloads':path.join(os.homedir(),'Downloads','GentlyPickModels')};
 }
 function registered(file,name){config.imports[file]=name;save();}
 return {scan,remember,registered};
}
module.exports={createInventory,modelName};
