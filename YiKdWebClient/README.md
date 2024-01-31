# YiKdWebClient代码示例及介绍

实现金蝶云星空第三方授权登录 使用纯HTTP协议实现
移除了对官方SDK的依赖 
移除了对Newtonsoft.Json的依赖

调用方式简单
如下示例:
# 1.第三方授权认证



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


# 2.旧版用户名密码认证
 
```
///2.旧版用户名密码认证
 string Formid = "SEC_User";
 string Json =@"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
 YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
 yiK3CloudClient.LoginType= LoginType.ValidateLogin;
 yiK3CloudClient.validateLoginSettingsModel=new ValidateLoginSettingsModel() { DbId= "629bd5285d655d", UserName="demo",Password="123456",lcid=2052};
 var resultJson = yiK3CloudClient.View(Formid, Json);
```

# 3.API签名认证

~~~
string Formid = "SEC_User";
string Json = @"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
yiK3CloudClient.LoginType=LoginType.LoginByApiSignHeaders;
var resultJson = yiK3CloudClient.View(Formid, Json);         
~~~
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
netstandard 2.0;
netstandard 2.1;
netframework 4.8 ;
netframework 4.8.1 ;
netcore 6.0;
netcore 8.0;

# 项目的依赖项
System.Net.Http;      System.Text.Json;      System.Security.Cryptography.Cng;

# nuget地址:
https://www.nuget.org/packages/YiKdWebClient

# nuget包的使用方法
![输入图片说明](nuget%E4%BD%BF%E7%94%A8.png)

# 配置文件路径
配置路如下  YiKdWebCfg/appsettings.xml 

# 配置文件内容
注意：(最新公有云可能强制要求走网关(https://api.kingdee.com/galaxyapi/)
走网关的方式需要使用API签名认证的模式。

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

    <!-- 服务Url地址(私有云必须配置金蝶云星空产品地址，K3Cloud/结尾。若为公有云则必须置空)-->

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

