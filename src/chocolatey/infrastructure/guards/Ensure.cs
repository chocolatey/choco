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
        public string Name { get; private set; }
        public EnsurableType Value { get; private set; }

        public Ensure(string name, EnsurableType value)
        {
            Name = name;
            Value = value;
        }

        public void is_not_null()
        {
            if (Value == null)
            {
                throw new ArgumentNullException(Name, "Value for {0} cannot be null.".format_with(Name));
            }
        }

        public void meets(Func<EnsurableType, bool> ensureFunction, Action<string, EnsurableType> exceptionAction)
        {
            Ensure.that(() => ensureFunction).is_not_null();
            Ensure.that(() => exceptionAction).is_not_null();

            if (!ensureFunction(Value))
            {
                exceptionAction.Invoke(Name, Value);
            }
        }
    }
}