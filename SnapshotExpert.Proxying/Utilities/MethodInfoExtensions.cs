using System.Linq.Expressions;
using System.Reflection;

namespace SnapshotExpert.Remoting.Utilities;

public static class MethodInfoExtensions
{
    extension(MethodInfo self)
    {
        public Type DelegateType
        {
            get
            {
                var types = self
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .Append(self.ReturnType)
                    .ToArray();
                return Expression.GetDelegateType(types.ToArray());
            }
        }
    }
}