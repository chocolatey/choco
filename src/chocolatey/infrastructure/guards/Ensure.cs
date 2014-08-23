namespace chocolatey.infrastructure.guards
{
    using System;
    using System.Linq.Expressions;

    public static class Ensure
    {
        public static Ensure<TypeToEnsure> that<TypeToEnsure>(Expression<Func<TypeToEnsure>> expression) where TypeToEnsure : class
        {
            var memberName = expression.get_name_on_right().Member.Name;
            return new Ensure<TypeToEnsure>(memberName, expression.Compile().Invoke());
        }

        private static MemberExpression get_name_on_right(this Expression e)
        {
            if (e is LambdaExpression)
                return get_name_on_right(((LambdaExpression)e).Body);

            if (e is MemberExpression)
                return (MemberExpression)e;

            if (e is MethodCallExpression)
            {
                var callExpression = (MethodCallExpression)e;
                var member = callExpression.Arguments.Count > 0 ? callExpression.Arguments[0] : callExpression.Object;
                return get_name_on_right(member);
            }

            if (e is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)e;
                return get_name_on_right(unaryExpression.Operand);
            }

            throw new Exception("Unable to find member for {0}".format_with(e.to_string()));
        }
    }

    public class Ensure<EnsurableType> where EnsurableType : class
    {
        private readonly string _name;
        private readonly EnsurableType _value;

        public Ensure(string name, EnsurableType value)
        {
            _name = name;
            _value = value;
        }

        public void is_not_null()
        {
            if (_value == null)
            {
                throw new ArgumentNullException("Ensure found that {0} ({1}) was null.".format_with(_name, typeof(EnsurableType).Name));
            }
        }
    }
}