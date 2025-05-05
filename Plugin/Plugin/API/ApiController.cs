using CFIT.AppLogger;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PilotsDeck.Plugin.API
{
    public class ApiController
    {
        public HttpListener HttpListener { get; }
        public static string Url => $@"http://localhost:{App.Configuration.ApiPortNumber}/";
        public static VariableManager VariableManager => App.PluginController.VariableManager;
        public static SimController SimController => App.SimController;

        public ApiController()
        {
            HttpListener = new();
        }

        public async virtual void Run()
        {
            HttpListener.Prefixes.Add(Url);
            HttpListener.Start();
            Logger.Information("ApiTask started");

            while (!App.CancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await HttpListener.GetContextAsync();
                try
                {
                    if (context.Request.HttpMethod == "GET" && context.Request.RawUrl.StartsWith("/v1/"))
                        await HandleGetRequestV1(context);
                    else
                    {
                        Logger.Warning($"Received unknown Request: {context.Request.RawUrl} (Method: {context.Request.HttpMethod})");
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                        Logger.LogException(ex);
                }

                try { context?.Response?.Close(); } catch { }
            }

            try { HttpListener?.Close(); } catch { }
            Logger.Information("ApiTask ended");
        }

        protected virtual async Task HandleGetRequestV1(HttpListenerContext context)
        {
            string url = context.Request.RawUrl.Replace("/v1/", "");
            Logger.Debug($"Handling Request: {url}");
            if (url.StartsWith("get/"))
                await HandleVariableReadRequest(context, url.Replace("get/", ""));
            else if (url.StartsWith("register/"))
                HandleVariableRegisterRequest(context, url.Replace("register/", ""));
            else if (url.StartsWith("unregister/"))
                HandleVariableUnregisterRequest(context, url.Replace("unregister/", ""));
            else if (url.StartsWith("set/"))
                HandleVariableWriteRequest(context, url.Replace("set/", ""));
            else if (url.StartsWith("send/"))
                HandleCommandSendRequest(context, url.Replace("send/", ""));
            else if (url.StartsWith("vjoy/"))
                HandleVjoySendRequest(context, url.Replace("vjoy/", ""));
            else if (url.StartsWith("list"))
                await HandleVariableListRequest(context);
            else
            {
                Logger.Warning($"Received unknown GET Request: {context.Request.RawUrl}");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            Logger.Debug($"Result: {context.Response.StatusCode}");
        }

        protected virtual void HandleVariableRegisterRequest(HttpListenerContext context, string variableName)
        {
            var address = new ManagedAddress(variableName);
            if (!VariableManager.TryGet(address, out _))
            {
                if (VariableManager.RegisterVariable(address) != null)
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                else
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        protected virtual void HandleVariableUnregisterRequest(HttpListenerContext context, string variableName)
        {
            var address = new ManagedAddress(variableName);
            if (VariableManager.TryGet(address, out _))
            {
                VariableManager.DeregisterVariable(address);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        protected virtual async Task HandleVariableReadRequest(HttpListenerContext context, string variableName)
        {
            var address = new ManagedAddress(variableName);
            if (address.ReadType != SimValueType.NONE && VariableManager.TryGet(address, out var variable))
            {
                string value = variable.Value;
                if (value.StartsWith("[[#"))
                    value = value[9..];
                await WriteResponse(context, value);
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        protected static async Task WriteResponse(HttpListenerContext context, string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK, string mimeType = "text/plain")
        {
            byte[] data = Encoding.UTF8.GetBytes(responseBody);
            context.Response.ContentType = mimeType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = data.LongLength;
            await context.Response.OutputStream.WriteAsync(data, App.CancellationToken);
            context.Response.StatusCode = (int)statusCode;
        }

        protected virtual void HandleVariableWriteRequest(HttpListenerContext context, string variableAssignment)
        {
            var idx = variableAssignment.IndexOf('=');
            if (idx == -1 || string.IsNullOrWhiteSpace(variableAssignment) || idx >= variableAssignment.Length)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var address = new ManagedAddress(variableAssignment[0..idx]);
            var value = WebUtility.UrlDecode(variableAssignment[(idx + 1)..]);
            if (address.ReadType != SimValueType.NONE && VariableManager.TryGet(address, out var variable))
            {
                variable.SetValue(value);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        protected virtual void HandleCommandSendRequest(HttpListenerContext context, string commandAddress)
        {
            SimCommandType? actionType = TypeMatching.GetCommandOnlyType(commandAddress);
            if (actionType == null || actionType == SimCommandType.VJOY || actionType == SimCommandType.VJOYDRV)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            SimCommand command = new()
            {
                Address = new ManagedAddress(commandAddress, (SimCommandType)actionType, true),
                Type = (SimCommandType)actionType,
                IsUp = true,
            };
            
            SimController.CommandChannel.TryWrite(command);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        protected virtual void HandleVjoySendRequest(HttpListenerContext context, string vjoyUrl)
        {
            string[] parts = vjoyUrl?.Split('/');

            if (parts?.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            string name = $"vjoy:{parts[0]}";
            string operation = parts[1].ToLowerInvariant();

            SimCommandType? actionType = TypeMatching.GetCommandOnlyType(name);
            if (actionType == null || (actionType != SimCommandType.VJOY && actionType != SimCommandType.VJOYDRV))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            bool isUp = true;
            if (operation == "down")
                isUp = false;
            else if (operation == "toggle")
                name = $"{name}:t";

            SimCommand command = new()
            {
                Address = new ManagedAddress(name, (SimCommandType)actionType, true),
                Type = (SimCommandType)actionType,
                IsUp = isUp,
            };
            SimController.CommandChannel.TryWrite(command);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        protected virtual async Task HandleVariableListRequest(HttpListenerContext context)
        {
            var variableList = VariableManager.VariableList.Where(v => v.IsSubscribed && v.Registrations > 0);
            StringBuilder body = new();

            foreach (var variable in variableList)
                body.AppendLine(variable.Address.ToString());

            await WriteResponse(context, body.ToString());
        }
    }
}
