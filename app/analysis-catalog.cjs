const methods=[
 {id:'coverage',name:'样本覆盖统计',purpose:'统计本次纳入的网页数、来源域名及缺失情况。',requires:'已采集网页和可追溯URL。',limit:'只代表本次采集样本，不等于全网覆盖或市场份额。'},
 {id:'facts',name:'5W1H事实框架',purpose:'按人物、事件、时间、地点、原因、方式整理原文。',requires:'原文明确披露的信息。',limit:'缺失的原因或时间应留空；来源的说法不等于已证实事实。'},
 {id:'keywords',name:'关键词频次',purpose:'统计所选主题词在采集文本中的出现次数。',requires:'主题词和纳入的文本。',limit:'词频不等于重要性、热度或用户态度；长文可能有偏重。'},
 {id:'topics',name:'主题编码 / 内容分析',purpose:'按明确分类口径归纳主题、事件和主要观点。',requires:'多篇文本与一致的分类定义。',limit:'分类包含模型判断，需抽查，不把提及次数当作支持人数。'},
 {id:'timeline',name:'事件时间线',purpose:'按原文中的事件日期排列进展。',requires:'可确定的事件日期，最好有多个时间点。',limit:'事件日期、文章发布日期和采集时间必须区分；未知日期不猜测。'},
 {id:'numbers',name:'原文数值与单位抽取',purpose:'列出数值、单位、对象、时间和原文证据。',requires:'原文中的可核对数字。',limit:'抽取不等于统计汇总；不同对象、口径、币种和时间不能混算。'},
 {id:'compare',name:'同口径对比矩阵',purpose:'按同一维度对比产品、机构或方案。',requires:'明确对象、统一单位与时间范围的可比数据。',limit:'不可比或缺失项明确标记，不编造排名。'},
 {id:'trend',name:'描述性趋势分析',purpose:'比较连续时间点的同口径变化，说明可能的方向。',requires:'同一指标的多个可比时间点及基期。',limit:'证据不足只描述观察，不做预测；没有基期不算同比/环比。'},
 {id:'risk',name:'风险登记表',purpose:'列出风险信号、证据、潜在影响和待核实事项。',requires:'明确的风险事件或可解释的信号。',limit:'影响判断属于推断，不凭文本生成精确概率或风险分数。'},
 {id:'swot',name:'SWOT证据矩阵',purpose:'按优势、劣势、机会、威胁组织证据。',requires:'明确分析对象，且有内外部信息。',limit:'不能从单篇宣传稿推导完整SWOT；空缺项必须保留。'},
 {id:'gaps',name:'证据缺口与一致性核对',purpose:'列出缺失字段、相互矛盾的说法和需要进一步确认的内容。',requires:'字段定义及相关原文；多来源有利于交叉核对。',limit:'转载同一消息不算多个独立证据，不自动宣布某来源真假。'}
];
const metric=(id,name,method,question)=>({id,name,method,question});
const categories=[
 {id:'brief',name:'资讯整理分析报表',description:'适合每日资讯、行业动态和专题追踪。',metrics:[metric('coverage','来源与样本覆盖','coverage','纳入了哪些资料？'),metric('facts','核心事实','facts','发生了什么，涉及谁，时间地点是什么？'),metric('topics','主题与观点','topics','主要讨论哪些主题，各方观点是什么？'),metric('timeline','事件进展','timeline','事件按什么顺序发展？'),metric('gaps','待核实事项','gaps','缺少什么证据，哪里存在不同说法？')]},
 {id:'compare',name:'产品与竞品对比报表',description:'适合显卡、软件、服务等对象的资料对比。',metrics:[metric('facts','对象与背景','facts','明确对比对象和适用场景。'),metric('numbers','参数与数值','numbers','原文披露了什么参数、价格或性能数据？'),metric('compare','同口径对比','compare','按相同维度列出差异和缺失项。'),metric('swot','优势与机会','swot','哪些优势、劣势、机会与威胁有证据支持？'),metric('gaps','可比性与缺口','gaps','哪些数据不在同一单位、时间或测试条件下？')]},
 {id:'trend',name:'趋势与风险观察报表',description:'适合持续追踪同一主题的进展及风险信号。',metrics:[metric('coverage','样本范围','coverage','本次样本覆盖范围。'),metric('timeline','事件时间线','timeline','原文披露的事件日期与进展。'),metric('trend','变化方向','trend','是否存在同口径的多时间点数据，能支持什么观察？'),metric('risk','风险信号','risk','风险、证据、潜在影响及核实需求。'),metric('gaps','结论边界','gaps','哪些判断还不能成立？')]},
 {id:'data',name:'数据指标摘录报表',description:'先清楚地保存原文数据，避免把不相关的数字混算。',metrics:[metric('coverage','数据来源','coverage','统计本次资料范围。'),metric('numbers','数值与单位明细','numbers','列出原文数字、单位、对象、时间和上下文。'),metric('keywords','主题词频','keywords','统计本任务主题词的文本出现次数。'),metric('gaps','数据质量','gaps','缺失单位、时间、统计口径或基数的项目。')]}
];
function validate(input){if(!Array.isArray(input)||!input.length||input.length>30)throw Error('报表大类应为1至30个');const ids=new Set();return input.map(c=>{const id=String(c.id||''),name=String(c.name||'').trim();if(!/^[\w-]{1,80}$/.test(id)||ids.has(id)||!name||name.length>80)throw Error('大类名称或标识无效');ids.add(id);if(!Array.isArray(c.metrics)||!c.metrics.length||c.metrics.length>30)throw Error('每个大类需要1至30个数据小类');const seen=new Set();return {id,name,description:String(c.description||'').slice(0,500),metrics:c.metrics.map(m=>{const mid=String(m.id||'');if(!/^[\w-]{1,80}$/.test(mid)||seen.has(mid)||!String(m.name||'').trim()||!methods.some(x=>x.id===m.method))throw Error('数据小类或分析方法无效');seen.add(mid);return {id:mid,name:String(m.name).trim().slice(0,80),method:m.method,question:String(m.question||'').slice(0,600)};})};});}
module.exports={methods,categories,validate};
