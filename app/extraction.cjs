// Small dependency-free HTML tree reader. Extraction uses article boundaries,
// never evaluates scripts or imports page markup into the application.
const decode=s=>String(s||'').replace(/&#(x[\da-f]+|\d+);|&(amp|lt|gt|quot|apos|nbsp|copy);/gi,(_,n,e)=>{if(e)return {amp:'&',lt:'<',gt:'>',quot:'"',apos:"'",nbsp:' ',copy:'©'}[e.toLowerCase()];const v=n[0].toLowerCase()==='x'?parseInt(n.slice(1),16):Number(n);return v>0&&v<=0x10ffff?String.fromCodePoint(v):'';});
function parse(html){
 const root={tag:'root',attrs:{},children:[]},stack=[root];
 const cleaned=html.replace(/<!--[\s\S]*?-->/g,'').replace(/<(script|style|noscript|svg)\b[^>]*>[\s\S]*?<\/\1\s*>/gi,'');
 for(const token of cleaned.match(/<[^>]+>|[^<]+/g)||[]){if(token.startsWith('</')){const tag=token.slice(2).match(/^[\w-]+/)?.[0]?.toLowerCase();for(let i=stack.length-1;i>0;i--)if(stack[i].tag===tag){stack.length=i;break;}}else if(/^<[a-z]/i.test(token)){const tag=token.slice(1).match(/^[\w-]+/)[0].toLowerCase(),attrs={};const rest=token.slice(tag.length+1);for(const m of rest.matchAll(/([^\s=/>]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g))attrs[m[1].toLowerCase()]=decode(m[2]??m[3]??m[4]??'');const node={tag,attrs,children:[]};stack.at(-1).children.push(node);if(!/^(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)$/.test(tag)&&!token.endsWith('/>'))stack.push(node);}else if(!token.startsWith('<'))stack.at(-1).children.push(decode(token));}
 return root;
}
const key=n=>`${n.attrs.id||''} ${n.attrs.class||''} ${n.attrs['data-testid']||''}`.toLowerCase();
function unwanted(n){return /^(nav|header|footer|aside|form|button|iframe|dialog|script|style|noscript|svg)$/.test(n.tag)||n.attrs.hidden!==undefined||n.attrs['aria-hidden']==='true'||/display\s*:\s*none/i.test(n.attrs.style||'')||/comment|recommend|related|advert|banner|sidebar|social|share|toolbar|copyright|promotion|sponsor|author-avatar|(^|[\s_-])(ads?|avatar|menu|login|feedback)([\s_-]|$)/i.test(key(n));}
function plain(n){if(typeof n==='string')return n;if(unwanted(n))return '';return n.children.map(plain).join('')+(/^(p|div|h[1-6]|li|tr|section|article|br|hr|figcaption)$/.test(n.tag)?'\n':'');}
function codeText(n){if(typeof n==='string')return n;if(/^(script|style|noscript|svg)$/.test(n.tag))return '';if(n.tag==='br')return '\n';return n.children.map(codeText).join('')+(/^(div|p|li)$/.test(n.tag)?'\n':'');}
function tableRows(table){
 const rows=[];
 function visit(n){if(typeof n==='string'||unwanted(n))return;if(n!==table&&n.tag==='table')return;if(n.tag==='tr'){
  const cells=n.children.filter(c=>typeof c!=='string'&&/^(td|th)$/.test(c.tag)&&!unwanted(c)).map(c=>({text:plain(c).replace(/[ \t\r]+/g,' ').replace(/ *\n */g,'\n').trim(),header:c.tag==='th',colspan:Math.max(1,Math.min(100,parseInt(c.attrs.colspan,10)||1)),rowspan:Math.max(1,Math.min(100,parseInt(c.attrs.rowspan,10)||1))}));
  if(cells.length)rows.push(cells);return;
 }for(const child of n.children)visit(child);}
 visit(table);return rows;
}
function structured(body){
 const pieces=[];let marker='\uE000GENTLYPICK';while(plain(body).includes(marker))marker+='X';
 function walk(n){if(typeof n==='string')return n;if(unwanted(n))return '';
  if(n.tag==='pre'){const text=codeText(n).replace(/\r\n?/g,'\n').replace(/^\n|\n$/g,'');if(text.trim()){
   const child=n.children.find(c=>typeof c!=='string'&&c.tag==='code');const hint=String(child?.attrs.class||n.attrs.class||'')+' '+String(child?.attrs['data-language']||n.attrs['data-language']||'');const language=(hint.match(/(?:language|lang)-([\w+#.-]+)/i)||[])[1]||'';
   const id=pieces.push({type:'code',text,language:language.slice(0,40)})-1;return '\n'+marker+id+'\uE001\n';}}
  if(n.tag==='table'){const rows=tableRows(n);if(rows.length){const id=pieces.push({type:'table',rows})-1;return '\n'+marker+id+'\uE001\n';}}
  return n.children.map(walk).join('')+(/^(p|div|h[1-6]|li|tr|section|article|br|hr|figcaption)$/.test(n.tag)?'\n':'');
 }
 const normalized=walk(body).replace(/[ \t\r]+/g,' ').split('\n').map(x=>x.trim()).filter(x=>x&&!/^(举报[\/／]反馈|收藏|分享|复制链接|扫码分享至微信|手机看|顶部|发表评论|全部评论|相关推荐|广告|关注|百度首页|登录)$/.test(x)).join('\n');
 if(!pieces.length)return {text:normalized,blocks:[]};
 const matcher=new RegExp(marker+'(\\d+)\uE001','g'),blocks=[];let pos=0,m;
 while((m=matcher.exec(normalized))){if(m.index>pos)blocks.push({type:'text',text:normalized.slice(pos,m.index)});const piece=pieces[Number(m[1])];if(piece)blocks.push(piece);pos=matcher.lastIndex;}
 if(pos<normalized.length)blocks.push({type:'text',text:normalized.slice(pos)});
 const text=blocks.map(b=>b.type==='table'?b.rows.map(row=>row.map(c=>c.text).join('\t')).join('\n'):b.text).join('');
 return {text,blocks};
}
function all(n,list=[]){if(typeof n!=='string'&&!unwanted(n)){list.push(n);for(const c of n.children)all(c,list);}return list;}
function extract(html,base='https://example.invalid/'){
 const tree=parse(html),nodes=all(tree);const titleNode=nodes.find(n=>n.tag==='title');const title=titleNode?plain(titleNode).trim():'网页摘录';
 const candidates=nodes.filter(n=>n.tag==='article'||n.tag==='main'||n.attrs.role==='main'||n.attrs.itemprop==='articleBody'||/(^|[\s_-])(article[-_]?body|article[-_]?content|entry[-_]?content|post[-_]?content|news[-_]?content)([\s_-]|$)/.test(key(n))||n.attrs['data-testid']==='article');
 const ranked=candidates.map(n=>({n,length:plain(n).trim().length,priority:n.attrs['data-testid']==='article'||n.attrs.itemprop==='articleBody'?3:n.tag==='article'?2:n.tag==='main'?0:1})).filter(x=>x.length>0).sort((a,b)=>b.priority-a.priority||b.length-a.length);
 const body=ranked[0]?.n||nodes.find(n=>n.tag==='body')||tree;
 const {text,blocks}=structured(body);
 if(!text)throw Error('未提取到文字，网页可能需要登录或 JavaScript 渲染');
 const seen=new Set(),images=[];
 for(const n of all(body)){if(n.tag!=='img')continue;const a=n.attrs;if((a.width&&parseInt(a.width)<80)||(a.height&&parseInt(a.height)<60)||/avatar|icon|logo|qrcode|qr-code|tracking|spacer/i.test((a.src||'')+' '+key(n)))continue;const src=a['data-original']||a['data-src']||a['data-lazy-src']||a.src||(a.srcset||'').split(',')[0].trim().split(/\s/)[0];if(!src)continue;try{const url=new URL(src,base);if(!['http:','https:'].includes(url.protocol)||seen.has(url.href))continue;seen.add(url.href);images.push({url:url.href,alt:(a.alt||'').slice(0,300)});}catch{}if(images.length>=20)break;}
 const linkSets={article:[],related:[]},linkSeen={article:new Set(),related:new Set()};
 const readLinks=(n,kind)=>{if(typeof n==='string')return;if(/^(script|style|nav|header|footer|form|button|svg)$/.test(n.tag)||/comment|advert|banner|sponsor|social|share|(^|[\s_-])(ads?|login|menu)([\s_-]|$)/i.test(key(n)))return;if(kind==='article'&&/recommend|related/i.test(key(n)))return;
  if(n.tag==='a'&&n.attrs.href){try{const u=new URL(n.attrs.href,base);u.hash='';if(['http:','https:'].includes(u.protocol)&&!u.username&&!u.password&&u.href!==new URL(base).href&&!linkSeen[kind].has(u.href)&&!/\.(pdf|zip|png|jpe?g|gif|mp4)(\?|$)/i.test(u.href)){const title=plain(n).trim();if(title){linkSeen[kind].add(u.href);linkSets[kind].push({url:u.href,title:title.slice(0,300)});}}}catch{}}
  for(const c of n.children)readLinks(c,kind);
 };
 readLinks(body,'article');
 const findRelated=n=>{if(typeof n==='string')return;if(/^(script|style|nav|header|footer|form|button)$/.test(n.tag)||/comment|advert|banner|sponsor/i.test(key(n)))return;if(/recommend|related|猜你喜欢|相关推荐|相关阅读/i.test(key(n))){readLinks(n,'related');return;}let relatedAfterHeading=false;for(const c of n.children){if(typeof c!=='string'&&/^h[1-6]$/.test(c.tag))relatedAfterHeading=/^(相关推荐|相关阅读|相关文章|猜你喜欢|Related articles|Recommended)/i.test(plain(c).trim());if(relatedAfterHeading)readLinks(c,'related');else findRelated(c);}};
 findRelated(tree);
 return {title,text,blocks,images,links:{article:linkSets.article.filter(x=>!linkSeen.related.has(x.url)).slice(0,100),related:linkSets.related.slice(0,100)}};
}
module.exports={extract,parse,decode};
