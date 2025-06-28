using TiktokenSharp;

namespace Jiro.Core.Utils;

public class Tokenizer
{
	public static async Task<int> CountTokensAsync(string input)
	{
		TikToken tokenizer = await TikToken.EncodingForModelAsync(Constants.AI.Gpt4oMiniModel);
		return tokenizer.Encode(input).Count;
	}
}
