using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Abstractions.Config;

namespace WebAppProxy.Config
{
    internal class MyTransformFactory : ITransformFactory
    {
        public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
        {
            throw new NotImplementedException();
        }

        public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
        {
            throw new NotImplementedException();
        }
    }
}
