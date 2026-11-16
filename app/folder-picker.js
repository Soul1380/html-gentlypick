(()=>{
const fields={taskDownloadDirectory:{title:'选择任务下载目录'},defaultDownloadDirectory:{title:'选择默认下载目录'},reportDirectory:{title:'选择报告导出目录'},directory:{title:'选择模型保存目录'},whisperDirectory:{title:'选择 Whisper 模型目录'},pythonPath:{kind:'file',title:'选择 Python 解释器',filter:'Python (python.exe)|python.exe'}};
function attach(){for(const [id,options] of Object.entries(fields)){const input=document.getElementById(id);if(input&&!input.dataset.uiPath)GPUI.path(input,options)}}
attach();new MutationObserver(attach).observe(document.body,{childList:true,subtree:true});
})();
