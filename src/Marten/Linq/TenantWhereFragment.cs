using Marten.Schema.Arguments;
using Marten.Storage;
using Marten.Util;

namespace Marten.Linq
{
    public class TenantWhereFragment: IWhereFragment
    {
        public static readonly string Filter = $"d.{TenantIdColumn.Name} = :{TenantIdArgument.ArgName}";

        public void Apply(CommandBuilder builder)
        {
            builder.Append(Filter);
            builder.AddNamedParameter(TenantIdArgument.ArgName, "");
        }

        public bool Contains(string sqlText)
        {
            return Filter.Contains(sqlText);
        }
    }
}
