using System.Text.Json.Serialization;
using Jiro.Core.Base.Results;

namespace Jiro.Core;

[JsonDerivedType(typeof(GraphResult))]
[JsonDerivedType(typeof(TextResult))]
public interface ICommandResult { }
