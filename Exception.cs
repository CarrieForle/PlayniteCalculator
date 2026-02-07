using System;

namespace Calculator
{
	internal class CalculatorException : Exception
	{
		public CalculatorException() : base() { }
		public CalculatorException(string message) : base(message) { }
		public CalculatorException(string message, Exception inner) : base(message, inner) { }
	}
}
