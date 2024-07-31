using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ogle
{
    public class CompressResponseAttribute : MiddlewareFilterAttribute
    {
        public CompressResponseAttribute() : base(typeof(CompressResponseAttribute))
        {
        }

        public void Configure(IApplicationBuilder builder)
        {
            var settings = builder.ApplicationServices.GetService<IOptions<OgleOptions>>();

            if (settings.Value.UseResponseCompression && builder.ApplicationServices.GetService<IResponseCompressionProvider>() != null)
            {
                builder.UseResponseCompression();
            }
        }
    }
}

