const assert=require('node:assert/strict');
const m=require('../app/collection-types.cjs');
const video=require('../app/video-tasks.cjs');
const source=require('../app/video-source.cjs');
(async()=>{
 const links=m.links('<a href="/a.pdf">PDF</a><a href="/a.pdf#x">重复</a><a href="javascript:a.pdf">无效</a><a href="/a.xlsx?q=1">表格</a>','https://example.com/page');
 assert.equal(links.length,2);assert.throws(()=>m.options({limit:101}));assert.throws(()=>m.options({maxMB:0}));
 assert(m.csv([{text:'=1+1'}]).includes("'=1+1"));assert(m.csv([{text:'a"b'}]).includes('a""b'));
 let pages=0;
 const get=async url=>({status:200,text:JSON.stringify(url.includes('/view?')?{code:0,data:{aid:1,title:'测试'}}:{code:0,data:{replies:[{rpid:++pages,member:{uname:'作者'},content:{message:'评论'},replies:[{rpid:100+pages,content:{message:'回复'}}]}],cursor:{next:pages,is_end:pages===3}}})});
 const result=await m.comments({url:'https://www.bilibili.com/video/BV123456',collectionOptions:{limit:5}},get);
 assert.equal(result.rows.length,5);assert.equal(pages,3);assert.equal(result.rows[1].parentId,'1');
 await assert.rejects(m.comments({url:'https://www.bilibili.com/bangumi/play/ep401765'},get),/电影评论暂不支持/);
 assert.equal(video.canonical('https://www.bilibili.com/bangumi/play/ep401765?theme=movie'),'https://www.bilibili.com/bangumi/play/ep401765');
 assert.equal(video.canonical('https://www.bilibili.com/bangumi/play/ss123'),'https://www.bilibili.com/bangumi/play/ss123');
 assert.throws(()=>source.assertComplete('Only preview format is available'),/试看/);assert.deepEqual(source.cookieArgs('chrome'),['--cookies-from-browser','chrome']);assert.deepEqual(source.cookieArgs(''),[]);
 console.log('PASS collection types and movie download regressions');
})().catch(e=>{console.error(e);process.exitCode=1});
