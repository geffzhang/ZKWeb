﻿namespace __ProjectName__.Console {
	using System;
	using System.IO;
	using ZKWeb;
	using ZKWeb.Testing;
	using ZKWeb.Testing.TestEventHandlers;

	public static class Program {
		public static void Main(string[] args) {
			RunTests();
		}

		public static void RunTests() {
			Application.Initialize(
				Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "../../../__ProjectName__.AspNet"));

			var testManager = Application.Ioc.Resolve<TestManager>();
			var testEventHandler = new TestConsoleEventHandler();
			testManager.RunAllAssemblyTest(testEventHandler);
			if (testEventHandler.CompletedInfo.Counter.Failed > 0) {
				throw new Exception("Some test failed");
			} else {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("All tests passed");
				Console.ResetColor();
			}
		}
	}
}
