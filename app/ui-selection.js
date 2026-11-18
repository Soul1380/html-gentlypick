/* Opt content containers in with .ui-selectable; controls stay unselectable. */
(()=>{'use strict';
const style=document.createElement('style');
style.textContent=`
html body { -webkit-user-select:none; user-select:none; }
html body .ui-selectable,html body .ui-selectable * { -webkit-user-select:text; user-select:text; }
html body input,html body textarea,html body [contenteditable]:not([contenteditable="false"]),html body [contenteditable]:not([contenteditable="false"]) * { -webkit-user-select:text; user-select:text; }
html body :is(button,select,option,summary,nav,[role="button"],.ui-unselectable),html body :is(button,select,option,summary,nav,[role="button"],.ui-unselectable) * { -webkit-user-select:none!important; user-select:none!important; }
html body .dragging,html body .dragging * { -webkit-user-select:none!important; user-select:none!important; }
`;
document.head.append(style);
const contentSelector='#reportReader,details[data-report-id^="analysis-"],#result .item,#summary,#log,pre,blockquote,.report-body,.article-content';
function mark(root){if(root.nodeType!==1)return;if(root.matches(contentSelector))root.classList.add('ui-selectable');root.querySelectorAll(contentSelector).forEach(n=>n.classList.add('ui-selectable'))}
let lastContent=null;
document.addEventListener('pointerdown',e=>{const target=e.target instanceof Element?e.target:null;lastContent=target?.closest('.ui-selectable')||null},true);
document.addEventListener('keydown',e=>{
if(!(e.ctrlKey||e.metaKey)||e.altKey||e.shiftKey||e.key.toLowerCase()!=='a')return;
const target=e.target instanceof Element?e.target:null;
if(target?.closest('input,textarea,[contenteditable]:not([contenteditable="false"])'))return;
e.preventDefault();
const modal=document.querySelector('dialog[open]');
const content=target?.closest('.ui-selectable')||(lastContent?.isConnected?lastContent:null);
if(!content||modal&&!modal.contains(content)||!content.getClientRects().length)return;
const range=document.createRange();range.selectNodeContents(content);
const selection=window.getSelection();selection.removeAllRanges();selection.addRange(range);
},true);
function start(){mark(document.body);new MutationObserver(records=>{for(const record of records)for(const node of record.addedNodes)mark(node)}).observe(document.body,{childList:true,subtree:true})}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',start,{once:true});else start();
})();
