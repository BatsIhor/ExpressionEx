using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionEx
{
    class Program
    {
        static void Main(string[] args)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof (int), "x");

            BinaryExpression binaryExpression = Expression.Multiply(parameterExpression, parameterExpression);

            Expression<Func<int, int>> lambdaExpression = Expression.Lambda<Func<int, int>>(binaryExpression, parameterExpression);

            Console.WriteLine(lambdaExpression);

            Func<int, int> compile = lambdaExpression.Compile();

            Console.WriteLine(compile(2));


            ExpressionCreator<Foo> expressionCreator = new ExpressionCreator<Foo>();

            //Dictionary<string, object> dict = new Dictionary<string, object>();
            //dict.Add("Name", "myName");
            //dict.Add("Value", 5);

            //Foo foo = expressionCreator.Create(dict);
        }
    }

    class ExpressionCreator<T>
    {
        private readonly Func<Dictionary<string, object>, T> _creator;

        public ExpressionCreator()
        {
            Type type = typeof(T);

            NewExpression newExpression = Expression.New(type); // new Foo()
            Console.WriteLine(newExpression);

            ParameterExpression dictParam = Expression.Parameter(typeof(Dictionary<string, object>), "d"); //d
            Console.WriteLine(dictParam);

            List<MemberBinding> list = new List<MemberBinding>();

            var propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

            foreach (var propertyInfo in propertyInfos)
            {
                Expression call = Expression.Call(typeof(DictionaryExtension), "GetValue", new[] { propertyInfo.PropertyType }, new Expression[] { dictParam, Expression.Constant(propertyInfo.Name)});
                Console.WriteLine(call);            //d.GetValue("Name")

                MemberBinding mb = Expression.Bind(propertyInfo.GetSetMethod(), call);
                Console.WriteLine(mb);              //Name = d.GetValue("Name")

                list.Add(mb);
            }

            MemberInitExpression memberInitExpression = Expression.MemberInit(newExpression, list);
            Console.WriteLine(memberInitExpression);//new Foo() {Name = d.GetValue("Name"), Value = d.GetValue("Value")}

            Expression<Func<Dictionary<string, object>, T>> ex = Expression.Lambda<Func<Dictionary<string, object>, T>>(memberInitExpression, new[] { dictParam });

            Console.WriteLine(ex);                  //d => new Foo() {Name = d.GetValue("Name"), Value = d.GetValue("Value")}
            _creator = ex.Compile();
        }

        public T Create(Dictionary<string, object> props)
        {
            return _creator(props);
        }
    }

    static class DictionaryExtension
    {
        public static TType GetValue<TType>(this Dictionary<string, object> d, string name)
        {
            object value;
            return d.TryGetValue(name, out value) ? (TType)value : default(TType);
        }
    }

    class Foo
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
