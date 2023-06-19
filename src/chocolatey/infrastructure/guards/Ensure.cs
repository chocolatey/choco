// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.guards
{
    using System;
    using System.IO;
    using System.Linq.Expressions;

    public static class Ensure
    {
        public static EnsureString That(Expression<Func<string>> expression)
        {
            var memberName = expression.GetNameOnRight().Member.Name;
            return new EnsureString(memberName, expression.Compile().Invoke());
        }

        public static Ensure<TypeToEnsure> That<TypeToEnsure>(Expression<Func<TypeToEnsure>> expression) where TypeToEnsure : class
        {
            var memberName = expression.GetNameOnRight().Member.Name;
            return new Ensure<TypeToEnsure>(memberName, expression.Compile().Invoke());
        }

        // This method needs a beter name.
        private static MemberExpression GetNameOnRight(this Expression e)
        {
            if (e is LambdaExpression lambdaExpr)
                return GetNameOnRight(lambdaExpr.Body);

            if (e is MemberExpression memberExpr)
                return memberExpr;

            if (e is MethodCallExpression methodExpr)
            {
                var member = methodExpr.Arguments.Count > 0 ? methodExpr.Arguments[0] : methodExpr.Object;
                return GetNameOnRight(member);
            }

            if (e is UnaryExpression unaryExpr)
            {
                return GetNameOnRight(unaryExpr.Operand);
            }

            throw new Exception("Unable to find member for {0}".FormatWith(e.ToStringSafe()));
        }


#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static EnsureString that(Expression<Func<string>> expression)
            => That(expression);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static Ensure<TypeToEnsure> that<TypeToEnsure>(Expression<Func<TypeToEnsure>> expression) where TypeToEnsure : class
            => That(expression);
#pragma warning restore IDE1006
    }

    public class EnsureString : Ensure<string>
    {
        public EnsureString(string name, string value)
            : base(name, value)
        {
        }

        public EnsureString NotNullOrWhitespace()
        {
            NotNull();

            if (string.IsNullOrWhiteSpace(Value))
            {
                throw new ArgumentException(Name, "Value for {0} cannot be empty or only contain whitespace.".FormatWith(Name));
            }

            return this;
        }

        public EnsureString HasExtension(params string[] extensions)
        {
            var actualExtension = Path.GetExtension(Value);

            foreach (var extension in extensions)
            {
                if (extension.IsEqualTo(actualExtension))
                {
                    return this;
                }
            }

            throw new ArgumentException(Name, "Value for {0} must contain one of the following extensions: {1}".FormatWith(Name, string.Join(", ", extensions)));
        }
#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public EnsureString is_not_null_or_whitespace()
            => NotNullOrWhitespace();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public EnsureString has_any_extension(params string[] extensions)
            => HasExtension(extensions);
#pragma warning restore IDE1006
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

        public void NotNull()
        {
            if (Value == null)
            {
                throw new ArgumentNullException(Name, "Value for {0} cannot be null.".FormatWith(Name));
            }
        }

        public void Meets(Func<EnsurableType, bool> ensureFunction, Action<string, EnsurableType> exceptionAction)
        {
            Ensure.That(() => ensureFunction).NotNull();
            Ensure.That(() => exceptionAction).NotNull();

            if (!ensureFunction(Value))
            {
                exceptionAction.Invoke(Name, Value);
            }
        }
#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void is_not_null()
            => NotNull();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void meets(Func<EnsurableType, bool> ensureFunction, Action<string, EnsurableType> exceptionAction)
            => Meets(ensureFunction, exceptionAction);
#pragma warning restore IDE1006
    }
}
