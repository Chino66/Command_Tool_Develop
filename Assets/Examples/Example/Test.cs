using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CommandTool;
using Newtonsoft.Json.Linq;

public class Test : MonoBehaviour
{
    private ProcessProxy proxy;

    private void Start()
    {
        proxy = new ProcessProxy();
        proxy.Start();

//        CancellationTaskExample2();
//        tokenSource2?.Cancel();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Run Command"))
        {
            proxy.Run("ipconfig", (ctx) =>
            {
                var content = "";
                foreach (var msg in ctx.Messages)
                {
                    content += $"{msg}\n";
                }

                Debug.Log(content);
            });
        }

        if (GUILayout.Button("Command.Run"))
        {
            Command.Run("ipconfig", (ctx) =>
            {
                var content = "";
                foreach (var msg in ctx.Messages)
                {
                    content += $"{msg}\n";
                }

                Debug.Log(content);
            });
        }

        if (GUILayout.Button("Command.RunSync"))
        {
            RunAsync();
        }

        if (GUILayout.Button("cancel"))
        {
//            tokenSource2?.Cancel();
            tcs.SetCanceled();
        }
    }

    private JObject _jObject;
    private JArray _jArray;

    private async void RunAsync()
    {
//        string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
//        foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
//        {
//            Debug.Log($"{variable.Key}:{variable.Value}");
//        }
//
//        return;
//        Debug.Log(token);
//        await Command.RunAsync($"echo {token}", (ctx) =>
//        {
//            var content = "";
//            foreach (var msg in ctx.Messages)
//            {
//                Debug.Log("RunAsync:" + msg);
////                content += msg;
//            }
//        }, false);
////        
//
//        await Command.RunAsync("echo %GITHUB_TOKEN%", (ctx) =>
//        {
//            var content = "";
//            foreach (var msg in ctx.Messages)
//            {
//                Debug.Log("RunAsync:" + msg);
////                content += msg;
//            }
//        }, false);
//        
        var command =
            "curl -u Chino66:%GITHUB_TOKEN% -H \"Accept: application/vnd.github.v3+json\" https://api.github.com/users/chino66/packages?package_type=npm";
        await Command.RunAsync(command, (ctx) =>
        {
            var content = "";
            foreach (var msg in ctx.Messages)
            {
//                Debug.Log("RunAsync:" + msg);
                content += msg;
            }

            if (content != "")
            {
                _jArray = JArray.Parse(content);
                foreach (var child in _jArray.Children())
                {
                    Debug.Log($"{child["name"]}, {child["url"]}, {child["repository"]["description"]}");
                }
            }
        }, false);
    }

    CancellationTokenSource tokenSource2;

    private async void CancellationTaskExample()
    {
        tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;

        var task = Task.Run(() =>
        {
//            ct.ThrowIfCancellationRequested();

            bool moreToDo = true;
            Debug.Log("00");
            while (moreToDo)
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }
            }

            Debug.Log("11");
        }, tokenSource2.Token); // Pass same token to Task.Run.

//        tokenSource2.Cancel();

        try
        {
            Debug.Log("11.1");
            await task;
            Debug.Log("22");
        }
        catch (OperationCanceledException e)
        {
        }
        finally
        {
            tokenSource2.Dispose();
            Debug.Log("33");
        }

        Debug.Log("44");
    }

    private async void CancellationTaskExample2()
    {
        tokenSource2 = new CancellationTokenSource();

        await RunAsyncCancellation(tokenSource2);
    }

    private TaskCompletionSource<bool> tcs;

    private async Task RunAsyncCancellation(CancellationTokenSource tokenSource)
    {
//        var task = Task.Delay(10000, tokenSource.Token); 

        tcs = new TaskCompletionSource<bool>();
        var task = Task.WhenAny(Task.Delay(10000), tcs.Task);

        try
        {
            Debug.Log("RunAsyncCancellation 1");
            await task;
            Debug.Log("RunAsyncCancellation 2");
        }
        catch (OperationCanceledException e)
        {
            Debug.LogError(e);
        }
        finally
        {
            tokenSource.Dispose();
            Debug.Log("RunAsyncCancellation 3");
        }

        Debug.Log("RunAsyncCancellation 4");
    }
}