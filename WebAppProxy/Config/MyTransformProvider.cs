using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Abstractions.Config;

namespace WebAppProxy.Config
{
    internal class MyTransformProvider : ITransformProvider
    {
        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(transformContext =>
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"UserId\":").Append(DateTime.Now.ToString("yyyyMMddHHmmss")).Append(",");
                sb.Append("\"UserName\":").Append("\"hello\"");
                sb.Append("}");
                var user = sb.ToString();
                transformContext.ProxyRequest.Headers.Add("UserInfo", user);
                return default;
            });
        }

        public void ValidateCluster(TransformClusterValidationContext context)
        {
        }

        public void ValidateRoute(TransformRouteValidationContext context)
        {
        }
    }
}
