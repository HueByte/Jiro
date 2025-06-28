using TiktokenSharp;

namespace Jiro.Core.Utils;

/// <summary>
/// Provides utilities for counting tokens in text strings using the TikToken library.
/// </summary>
public class Tokenizer
{
	/// <summary>
	/// Counts the number of tokens in the specified input string using the GPT-4o mini model tokenizer.
	/// </summary>
	/// <param name="input">The text string to count tokens for.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the number of tokens in the input string.</returns>
	public static async Task<int> CountTokensAsync(string input)
	{
		TikToken tokenizer = await TikToken.EncodingForModelAsync(Constants.AI.Gpt4oMiniModel);
		return tokenizer.Encode(input).Count;
	}
}
