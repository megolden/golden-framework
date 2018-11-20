using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Golden.Data.Extensions
{
	public interface IMultipleResult
	{
		int ResultCount { get; }

		IEnumerable<TResult> GetResult<TResult>(int index);
	}
	internal class MultipleResult : IMultipleResult
	{
		private readonly List<System.Collections.IList> _Results;

		public int ResultCount
		{
			get { return _Results.Count; }
		}

		public MultipleResult(List<System.Collections.IList> results)
		{
			_Results = results;
		}
		public IEnumerable<TResult> GetResult<TResult>(int index)
		{
			if (index < 0 || index >= _Results.Count)
				throw new IndexOutOfRangeException();
			return (IEnumerable<TResult>)_Results[index];
		}
	}
}
