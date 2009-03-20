#region License
/*
 * NReco library (http://code.google.com/p/nreco/)
 * Copyright 2008 Vitaliy Fedorchenko
 * Distributed under the LGPL licence
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using NReco.Logging;

namespace NReco.Transform {
	
	/// <summary>
	/// XML file modification rule
	/// </summary>
	/// <remarks>
	/// This rule processes files that started with special char ('@' by default).
	/// It may contain single rule XML configuration 
	/// <code>
	/// <xml-insert file="somefile.text" xpath="/root">
	/// ***
	/// </xml-insert>
	/// </code>
	/// or multiple rules.
	/// </remarks>
	public class ModifyXmlFileRule : IFileRule {
		ILog log = LogManager.GetLogger(typeof(ModifyXmlFileRule));
		// TODO: extract similar functionality to shared FileRule base class

		public bool MatchFile(string filePath, IFileManager fm) {
			// match code should be ultra-fast: match rule is hardcoded.
			if (Path.GetFileName(filePath).StartsWith("@")) {
				string trimmed = fm.Read(filePath);
				return (trimmed.Contains("<xml-insert") && trimmed.Contains("</xml-insert>")) ||
					(trimmed.Contains("<xml-replace") && trimmed.Contains("</xml-replace>")) ||
					(trimmed.Contains("<xml-remove") && trimmed.Contains("</xml-remove>"));
			}
			return false;
		}

		public override string ToString() {
			return "Modify XML rule";
		}

		public void Execute(FileRuleContext ruleContext) {
			string filePath = ruleContext.RuleFileName;
			string fileContent = ruleContext.FileManager.Read(filePath);

            Mvp.Xml.XInclude.XIncludingReader xmlIncludeContentRdr = new Mvp.Xml.XInclude.XIncludingReader(new StringReader(fileContent));
            xmlIncludeContentRdr.XmlResolver = new FileManagerXmlResolver(ruleContext.FileManager, Path.GetDirectoryName(filePath));
            XPathDocument ruleXPathDoc = new XPathDocument(xmlIncludeContentRdr);
			XPathNavigator ruleNav = ruleXPathDoc.CreateNavigator();
			XPathNodeIterator ruleNavs = ruleNav.Select("/rules/*[starts-with(name(),'xml-')]|/*[starts-with(name(),'xml-')]");
			foreach (XPathNavigator ruleConfigNav in ruleNavs) {
				Config config = new Config();
				config.ReadFromXmlNode( ruleConfigNav );
				if (String.IsNullOrEmpty( config.TargetFile ))
					config.TargetFile = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Substring(1));
				ProcessFileRule(ruleContext, config );
			}
		}

		protected void ProcessFileRule(FileRuleContext ruleContext, Config config) {
			// target text file
			string targetFilePath = config.TargetFile;
			string targetFileContent = ruleContext.FileManager.Read(targetFilePath);

			// TODO: handle exceptions
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.PreserveWhitespace = true;
			xmlDoc.LoadXml(targetFileContent);
			// deal with namespaces
			IDictionary<string,string> namespaces = config.Xml.GetNamespacesInScope(XmlNamespaceScope.All);
			XmlNamespaceManager xmlNsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
			foreach (KeyValuePair<string,string> ns in namespaces) {
				xmlNsMgr.AddNamespace(ns.Key, ns.Value);
			}

			bool targetChanged = false;
			if (config.XPath!=null) {
				targetChanged = ApplyXPathRule(config, xmlDoc, xmlNsMgr);
			}

			if (targetChanged)
				ruleContext.FileManager.Write(targetFilePath, xmlDoc.OuterXml);
			else {
				log.Write(LogEvent.Warn, new {Msg = "Rule is not matched", Config = config });
			}
		}

		protected bool ApplyXPathRule(Config cfg, XmlDocument xmlDoc, XmlNamespaceManager xmlNsMgr) {
			if (cfg.RuleType == "xml-insert") {
				XmlNode targetNode = xmlDoc.SelectSingleNode(cfg.XPath, xmlNsMgr);
				if (targetNode!=null) {
					XPathNavigator targetNav = targetNode.CreateNavigator();
					foreach (XPathNavigator nav in cfg.Xml.SelectChildren(XPathNodeType.All)) {
						switch (cfg.InsertMode) {
							case Config.InsertModeType.Child:
								targetNav.AppendChild(nav);
								break;
							case Config.InsertModeType.Before:
								targetNav.InsertBefore(nav);
								break;
							case Config.InsertModeType.After:
								targetNav.InsertAfter(nav);
								break;
						}
					}
					return true;
				}
			} else if (cfg.RuleType=="xml-remove" || cfg.RuleType=="xml-replace") {
				// if replace - insert nodes first
				if (cfg.RuleType == "xml-replace") {
					XmlNode targetNode = xmlDoc.SelectSingleNode(cfg.XPath, xmlNsMgr);
					if (targetNode != null) {
						XPathNavigator targetNav = targetNode.CreateNavigator();
						foreach (XPathNavigator nav in cfg.Xml.SelectChildren(XPathNodeType.All))
							targetNav.InsertBefore(nav);
					}
				}

				XmlNodeList targetNodes = xmlDoc.SelectNodes(cfg.XPath, xmlNsMgr);
				foreach (XmlNode node in targetNodes)
					node.ParentNode.RemoveChild(node);
				return targetNodes.Count > 0;
			}
			return false;
		}

		public class Config {
			public enum InsertModeType { Child, Before, After };

			public InsertModeType InsertMode { get; set; }
			public string XPath { get; set; }

			public string RuleType { get; set; }

			public string TargetFile { get; set; }

			public XPathNavigator Xml { get; set; }

			public Config() {
			}

			public void ReadFromXmlNode(IXPathNavigable config) {
				XPathNavigator configNav = config.CreateNavigator();
				RuleType = configNav.Name;
				XPath = configNav.GetAttribute("xpath", String.Empty)!=String.Empty ? configNav.GetAttribute("xpath", String.Empty) : null;
				TargetFile = configNav.GetAttribute("file", String.Empty)!=String.Empty ? configNav.GetAttribute("file", String.Empty) : null;
				Xml = configNav;
				InsertMode = configNav.GetAttribute("mode", String.Empty) != String.Empty ? 
								(InsertModeType)Enum.Parse( typeof(InsertModeType), configNav.GetAttribute("mode", String.Empty), true) : InsertModeType.Child;
			}

			public override string ToString() {
				return String.Format("{{RuleType={0},XPath={1},TargetFile={2}}}", RuleType,XPath,TargetFile);
			}

		}

	}

}
