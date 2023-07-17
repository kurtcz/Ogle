namespace Ogle
{
	internal class NodeResponse<T>
	{
		public T? Payload { get; set; }
		public string? Error { get; set; }
	}
}

