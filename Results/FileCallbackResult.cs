using Microsoft.AspNetCore.Mvc;

namespace ApiLogDemo.Results
{
    public class FileCallbackResult : FileResult
    {
        private readonly Func<Stream, ActionContext, Task> _callback;

        public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;

            await _callback(response.Body, context);
        }
    }
}
