# CommandTool

项目地址：[https://github.com/Chino66/Command\_Tool\_Develop](https://github.com/Chino66/Command_Tool_Develop)

CommandTool是一个在Unity上使用cmd命令的工具。

## 使用方式

### 同步执行命令

    Command.Run("ipconfig", (ctx) =>
    {
        var content = "";
        foreach (var msg in ctx.Messages)
        {
            content += $"{msg}\n";
        }
        Debug.Log(content);
    });

### 异步执行命令

    var command = "ipconfig";
    await Command.RunAsync(command, (ctx) =>
    {
        var content = "";
        foreach (var msg in ctx.Messages)
        {
            content += $"{msg}\n";
        }
        Debug.Log(content);
    }, false);