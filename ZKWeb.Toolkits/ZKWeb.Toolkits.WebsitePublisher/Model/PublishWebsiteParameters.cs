﻿using System;

namespace ZKWeb.Toolkits.WebsitePublisher.Model {
	/// <summary>
	/// Publish website parameters
	/// </summary>
	public class PublishWebsiteParameters {
		/// <summary>
		/// Website root
		/// </summary>
		public string WebRoot { get; set; }
		/// <summary>
		/// Output name
		/// </summary>
		public string OutputName { get; set; }
		/// <summary>
		/// Output directory
		/// </summary>
		public string OutputDirectory { get; set; }
		/// <summary>
		/// Ignore pattern in regex
		/// </summary>
		public string IgnorePattern { get; set; }
		/// <summary>
		/// Which configuration to publish
		/// Default is "release"
		/// </summary>
		public string Configuration { get; set; }
		/// <summary>
		/// Which framework to publish
		/// Default is "net461"
		/// </summary>
		public string Framework { get; set; }

		/// <summary>
		/// Initialize
		/// </summary>
		public PublishWebsiteParameters() {
			Configuration = "release";
			Framework = "net461";
		}

		/// <summary>
		/// Check parameters
		/// </summary>
		public void Check() {
			if (string.IsNullOrEmpty(WebRoot)) {
				throw new ArgumentException("WebRoot can't be empty");
			} else if (string.IsNullOrEmpty(OutputName)) {
				throw new ArgumentException("OutputName can't be empty");
			} else if (string.IsNullOrEmpty(OutputDirectory)) {
				throw new ArgumentException("OutputDirectory can't be empty");
			} else if (string.IsNullOrEmpty(Configuration)) {
				throw new ArgumentException("Configuration can't be empty");
			} else if (string.IsNullOrEmpty(Framework)) {
				throw new ArgumentException("Framework can't be empty");
			}
		}
	}
}
