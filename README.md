# YiKdWebClient代码示例及介绍

实现金蝶云星空第三方授权登录 使用纯HTTP协议实现
移除了对官方SDK的依赖 
移除了对Newtonsoft.Json的依赖
# 配置文件路径
配置的相对路径如下  YiKdWebCfg/appsettings.xml ，用于依赖于第三方登录授权验证和API签名验证，也可以自己实例化YiK3CloudClient中的AppSettingsModel类

调用方式简单
如下示例:
# 1.第三方授权认证(需要设置配置文件:YiKdWebCfg/appsettings.xml)



```
///1.第三方授权认证
string Formid = "SEC_User";
string Json =@"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
var resultJson  = yiK3CloudClient.View(Formid, Json);
```



可以获取到请求的真实地址，和真实请求的body 如下图
![输入图片说明](%E8%AF%B7%E6%B1%82.png)

可以利用此信息，使用postman 等接口调试工具进行调试，更方便快捷。 也可以使用其它开发语言进行请求，原理一致


# 2.旧版用户名密码认证(不需要设置appsettings.xml)
 
```
///2.旧版用户名密码认证
 string Formid = "SEC_User";
 string Json =@"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
 YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
 yiK3CloudClient.LoginType= LoginType.ValidateLogin;
 yiK3CloudClient.validateLoginSettingsModel=new ValidateLoginSettingsModel() { Url = @"http://127.0.0.1/K3Cloud/", DbId= "629bd5285d655d", UserName="demo",Password="123456",lcid=2052};
 var resultJson = yiK3CloudClient.View(Formid, Json);
```
# 3.集成密钥文件认证(不需要设置appsettings.xml)
```
string Formid = "SEC_User";
string Json = @"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
yiK3CloudClient.LoginType = LoginType.LoginBySimplePassport;
string cnfFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "YiKdWebCfg", "API测试.cnf");
yiK3CloudClient.LoginBySimplePassportModel = new LoginBySimplePassportModel() { Url = @"http://127.0.0.1/K3Cloud/", CnfFilePath = cnfFilePath };
var resultJson = yiK3CloudClient.View(Formid, Json);
```

# 4.API签名认证(需要设置配置文件:YiKdWebCfg/appsettings.xml)

~~~
string Formid = "SEC_User";
string Json = @"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
yiK3CloudClient.LoginType=LoginType.LoginByApiSignHeaders;
var resultJson = yiK3CloudClient.View(Formid, Json);
Console.WriteLine(resultJson);
~~~
API签名认证的最大特点是，真实的请求中，没有调用登陆验证接口，web请求次数会大幅度降低


# 使用postman，Apipost 工具调试
如下为使用postman，Apipost 工具的方法
~~~
 //签名请求头的字符串，可以直接导入postman，Apipost
string RequestHeadersString = yiK3CloudClient.RequestHeadersString;
Console.WriteLine("签名请求头的字符串，可以直接导入postman，Apipost:");
Console.WriteLine(RequestHeadersString);
//真实的请求地址
string RequestUrl = yiK3CloudClient.ReturnOperationWebModel.RequestUrl;
Console.WriteLine("真实的请求地址: ");
Console.WriteLine(RequestUrl);
//真实的请求报文
string RealRequestBody = yiK3CloudClient.ReturnOperationWebModel.RealRequestBody;
Console.WriteLine("真实的请求报文: ");
Console.WriteLine(RealRequestBody);
Console.WriteLine("请求结果: ");
Console.WriteLine(resultJson);
~~~

运行结果如下:
![输入图片说明](API%E7%AD%BE%E5%90%8D%E6%A8%A1%E5%BC%8F%E8%BF%90%E8%A1%8C%E6%95%88%E6%9E%9C.png)

# JSON 格式
传入方法的JSON格式，与金蝶官方文档要求的格式完全一致. 注:(官方文档的JSON格式，并不是最终http请求的格式)


# 当前已经支持编译的版本
net9.0;net8.0;net7.0;net6.0;net5.0;net481;net48;net472;net471;net47;net462;netstandard2.1;netstandard2.0;

# 项目的依赖项
System.Net.Http;      System.Text.Json;      System.Security.Cryptography.Cng;

# nuget地址:
https://www.nuget.org/packages/YiKdWebClient

# nuget包的使用方法
![输入图片说明](nuget%E4%BD%BF%E7%94%A8.png)



# 配置文件内容(相对路径YiKdWebCfg/appsettings.xml 文件，如果没有就手动创建)
注意：(最新公有云可能强制要求走网关(https://api.kingdee.com/galaxyapi/)
走网关的方式需要使用API签名认证的模式。
最新询问总部（2024年10月)，目前不再强制公有云使用网关模式，公有云可以正常调用api，后续实际情况根据官方为准。框架功能里面已经全部包含，均可使用

```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>

  <appSettings>

    <!-- 当前使用的 账套ID(即数据中心id) -->

    <!-- 第三方系统登录授权的账套ID（即open.kingdee.com网站的第三方系统登录授权中的数据中心标识）-->

    <!-- 在第三方系统登录授权页面点击“生成测试链接”按钮后即可查看   -->

    <add key="X-KDApi-AcctID" value="629bd5285d655d"/>

    <!-- 第三方系统登录授权的 集成用户名称  -->

    <!-- 补丁版本为PT-146894 [7.7.0.202111]及后续的版本，则为指定用户登录列表中任一用户  -->

    <!-- 若第三方系统登录授权已勾选“允许全部用户登录”，则无以上限制  -->

    <add key="X-KDApi-UserName" value="Administrator"/>

    <!-- 第三方系统登录授权的 应用ID  -->

    <add key="X-KDApi-AppID" value="2********************P"/>

    <!-- 第三方系统登录授权的 应用密钥  -->

    <add key="X-KDApi-AppSec" value="a***********************7"/>

    <!-- 账套语系，默认2052  -->

    <add key="X-KDApi-LCID" value="2052"/>

    <!-- 组织编码，启用多组织时配置对应的组织编码才有效(使用签名模式认证有效，其他待测试，可以先不填写)  -->

    <!--<add key="X-KDApi-OrgNum" value="*****"/>-->

    <!-- 服务Url地址(私有云必须配置金蝶云星空产品地址，K3Cloud/结尾。若为需要走公有云网关模式,则必须置空)-->

    <add key="X-KDApi-ServerUrl" value="http://127.0.0.1/k3cloud/"/>
  </appSettings>

</configuration>
```
# 功能列表(功能名称与官方功能名方式相同，以此类推)

| 接口名称 | 接口含义 |
|------|------|
|Save|保存|
|BatchSave|批量保存|
|Audit|审核|
|Delete|删除|
|UnAudit|反审核|
|Submit|提交|
|View|查看|
|ExecuteBillQuery|单据查询|
|Draft|暂存|
|Allocate|分配|
|ExecuteOperation|操作接口|
|FlexSave|弹性域保存|
|SendMsg|发送消息|
|Push|下推|
|GroupSave|分组保存|
|Disassembly|拆单|
|QueryBusinessInfo|查询单据信息|
|QueryGroupInfo|查询分组信息|
|WorkflowAudit|工作流审批|
|GroupDelete|分组删除|
|CancelAllocate|取消分配|
|SwitchOrg|切换组织接口|
|CancelAssign|撤销服务接口|
|GetSysReportData|获取报表数据|
|AttachmentUpload|上传附件|
|AttachmentDownLoad|下载附件|

# 项目地址
https://gitee.com/lnsyzjw/yi-kd-web-client

# 单点登录功能V4的示例代码

# 单点登录V4调用示例代码:
```
#region 单点登录V4
SSOHelper sSOHelper = new SSOHelper() { Url= @"http://127.0.0.1:9980/K3Cloud" };
//sSOHelper.appSettingsModel.XKDApiUserName = "demo"; /*指定用户，若不指定则取配置文件默认的集成用户*/
sSOHelper.GetSsoUrlsV4();
#endregion
```
# 单点登录V4调用示例的返回结果:
```
#region 单点登录结果
 /*****如下为获取到的相关链接数据***********************************/
 //数据中心ID
 Console.WriteLine("数据中心ID："+" "+ sSOHelper.simplePassportLoginArg.dbid);
 //应用ID
 Console.WriteLine("应用ID：" + " " + sSOHelper.simplePassportLoginArg.appid);
 //用户名称
 Console.WriteLine("用户名称：" + " " + sSOHelper.simplePassportLoginArg.username);
 //时间戳
 Console.WriteLine("时间戳：" + " " + sSOHelper.timestamp);
 //签名
 Console.WriteLine("签名：" + " " + sSOHelper.simplePassportLoginArg.signeddata);
 //请求参数（json格式）
 Console.WriteLine("请求参数（json格式）：" + " " + sSOHelper.argJosn);
 //参数格式化（Base64）
 Console.WriteLine("参数格式化（Base64）：" + " " + sSOHelper.argJsonBase64);
 // Silverlight入口链接
 Console.WriteLine("Silverlight入口链接:");
 Console.WriteLine(sSOHelper.SSOLoginUrlObject.silverlightUrl);
 // html5入口链接
 Console.WriteLine("html5入口链接:");
 Console.WriteLine(sSOHelper.SSOLoginUrlObject.html5Url);
 //客户端入口链接
 Console.WriteLine("客户端入口链接:");
 Console.WriteLine(sSOHelper.SSOLoginUrlObject.wpfUrl);
 #endregion
```
# 自定义webapi
调用YiK3CloudClient中的CustomBusinessService方法 如下图
![输入图片说明](%E8%87%AA%E5%AE%9A%E4%B9%89webapi.png)

# 辅助工具函数
## 文件分块上传(直接返回最终结果)

```
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();

// 设置登录类型
yiK3CloudClient.LoginType = LoginType.LoginBySimplePassport;

// 配置文件路径
string cnfFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "YiKdWebCfg", "API测试.cnf");

// 设置登录信息
yiK3CloudClient.LoginBySimplePassportModel = new LoginBySimplePassportModel()
{
    Url = @"http://127.0.0.1/K3Cloud/",
    CnfFilePath = cnfFilePath
};

// 文件路径
string path = @"D:\test1.pdf";

// 创建上传模型
UploadModel uploadModelTemplate = new UploadModel();
uploadModelTemplate.data.FormId = "BD_Currency";
uploadModelTemplate.data.InterId = "143717";
uploadModelTemplate.data.BillNO = "测试编码";

// 上传附件
string resJson = AttachmentHelper.AttachmentUploadByFilePath(path, yiK3CloudClient, uploadModelTemplate, 1024 * 1024 * 2);

// 输出结果
Console.WriteLine(resJson);

```


## 文件分块上传(获取完整的上传过程)

```
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();

// 设置登录类型
yiK3CloudClient.LoginType = LoginType.LoginBySimplePassport;

// 配置文件路径
string cnfFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "YiKdWebCfg", "API测试.cnf");

// 设置登录信息
yiK3CloudClient.LoginBySimplePassportModel = new LoginBySimplePassportModel()
{
    Url = @"http://127.0.0.1/K3Cloud/",
    CnfFilePath = cnfFilePath
};

// 文件路径
string path = @"D:\test1.pdf";

// 创建上传模型
UploadModel uploadModelTemplate = new UploadModel();
uploadModelTemplate.data.FormId = "BD_Currency";
uploadModelTemplate.data.InterId = "143717";
uploadModelTemplate.data.BillNO = "测试编码";

// 定义上传进度回调
Action<FileChunk, YiK3CloudClient> progressAction = (fileChunk, yiK3CloudClient) =>
{
    Console.WriteLine("正在处理第" + (fileChunk.Chunkindex + 1) + "分块");
    Console.WriteLine("请求报文为:" + yiK3CloudClient.ReturnOperationWebModel.RealRequestBody);
    Console.WriteLine("处理结果为:" + yiK3CloudClient.ReturnOperationWebModel.RealResponseBody);
    if (fileChunk.IsLast)
    {
        Console.WriteLine("所有分块处理结束");
    }
};

// 上传附件
string resJson = AttachmentHelper.AttachmentUploadByFilePath(
    path, 
    yiK3CloudClient, 
    uploadModelTemplate, 
    1024 * 1024 * 2,  // 限制每个分块最大为 2MB
    progressAction
);

// 输出结果
Console.WriteLine(resJson);


```

## base64流分块上传辅助函数
AttachmentUploadByFilePath函数更换为AttachmentUploadByBase64



