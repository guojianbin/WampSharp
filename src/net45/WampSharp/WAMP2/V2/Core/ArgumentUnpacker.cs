using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2.Core
{
    internal class ArgumentUnpacker
    {
        private readonly LocalParameter[] mParameters;

        public ArgumentUnpacker(LocalParameter[] parameters)
        {
            mParameters = parameters;
        }

        public LocalParameter[] Parameters
        {
            get
            {
                return mParameters;
            }
        }

        public object[] UnpackParameters<TMessage>(IWampFormatter<TMessage> formatter,
            TMessage[] arguments,
            IDictionary<string, TMessage> argumentsKeywords)
        {
            IEnumerable<object> positional = Enumerable.Empty<object>();

            int positionalArguments = 0;

            if (!SkipPositionalArguments && (arguments != null))
            {
                positionalArguments = arguments.Length;

                positional =
                    Parameters.Take(positionalArguments)
                        .Zip(arguments, (parameter, value) => new { parameter, value })
                        .Select(x => GetPositionalParameterValue(formatter, x.parameter, x.value));
            }

            var named = Parameters.Skip(positionalArguments)
                .Select(parameter => GetNamedParameterValue(formatter, parameter, argumentsKeywords));

            object[] result = positional.Concat(named).ToArray();

            return result;
        }

        public bool SkipPositionalArguments { get; set; }

        private object ConvertParameter<TMessage>(IWampFormatter<TMessage> formatter, LocalParameter parameter, TMessage value)
        {
            return formatter.Deserialize(parameter.Type, value);
        }

        private object GetNamedParameterValue<TMessage>(IWampFormatter<TMessage> formatter, LocalParameter parameter, IDictionary<string, TMessage> argumentKeywords)
        {
            if (parameter.Name == null)
            {
                throw PositionError(parameter.Position);
            }
            else
            {
                TMessage value;

                if (argumentKeywords != null &&
                    argumentKeywords.TryGetValue(parameter.Name, out value))
                {
                    return ConvertNamedParameter(formatter, parameter, value);
                }
                else if (parameter.HasDefaultValue)
                {
                    return parameter.DefaultValue;
                }
                else
                {
                    throw NameError(parameter.Name);
                }
            }
        }

        private object ConvertNamedParameter<TMessage>(IWampFormatter<TMessage> formatter,
            LocalParameter parameter,
            TMessage value)
        {
            try
            {
                return ConvertParameter(formatter, parameter, value);
            }
            catch (Exception ex)
            {
                throw NameError(parameter.Name, ex);
            }
        }

        private object GetPositionalParameterValue<TMessage>(IWampFormatter<TMessage> formatter,
            LocalParameter parameter,
            TMessage value)
        {
            try
            {
                return ConvertParameter(formatter, parameter, value);
            }
            catch (Exception ex)
            {
                throw PositionError(parameter.Position, ex);
            }
        }

        protected virtual Exception NameError(string name, Exception exception = null)
        {
            return new WampInvalidArgumentException("argument name: " + name, exception);
        }

        protected virtual Exception PositionError(int position, Exception exception = null)
        {
            return new WampInvalidArgumentException("argument position: " + position, exception);
        }
    }
}