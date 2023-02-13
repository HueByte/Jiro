using System.Text.Json.Serialization;
using Jiro.Core.Base.Result;

namespace Jiro.Core.Base;

[JsonDerivedType(typeof(GraphResult))]
[JsonDerivedType(typeof(TextResult))]
public interface ICommandResult { }
