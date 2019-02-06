using System;
using System.AddIn;
using System.AddIn.Pipeline;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AddInSideViews;

namespace SIL.ParatextMapCreatorPlugin
{
    /// <summary>
    /// Simple plugin that shows a text box that the user can enter text into. The text is
    /// then persisted with the other Paratext project data.
    /// </summary>
    [AddIn(pluginName, Description = "Fills out the XML files needed by Ethnos 360's Map Creator using the place names from a project's biblical terms data.",
        Version = "1.0", Publisher = "SIL")]
    [QualificationData(PluginMetaDataKeys.menuText, "&" + pluginName + "...")]
    [QualificationData(PluginMetaDataKeys.insertAfterMenuName, "Tools|Advanced")]
    [QualificationData(PluginMetaDataKeys.menuImagePath, @"MapCreatorNamesGeneratorPlugin\icon.png")]
    [QualificationData(PluginMetaDataKeys.enableWhen, WhenToEnable.scriptureProjectActive)]
    [QualificationData(PluginMetaDataKeys.multipleInstances, CreateInstanceRule.forEachActiveProject)]
    public class MapCreatorNamesGeneratorPlugin : IParatextAddIn2
    {
        public const string pluginName = "Generate Ethnos 360 Map Creator Data";

        private IHost host;
        private string projectName;
        //private readonly XmlSerializer dataSerializer = new XmlSerializer(typeof(ProjectTextData));

        /// <summary>
        /// Called by Paratext when the menu item created for this plugin was clicked.
        /// </summary>
        public void Run(IHost ptHost, string activeProjectName)
        {
	        lock (this)
	        {
		        if (host != null)
		        {
			        // This should never happen, but just in case Host does something wrong...
			        ptHost.WriteLineToLog(this, "Run called more than once!");
			        return;
		        }
	        }

	        try
	        {
		        Application.EnableVisualStyles();
				host = ptHost;
				projectName = activeProjectName;

#if DEBUG
				MessageBox.Show("Attach debugger now (if you want to)", pluginName);
#endif
				ptHost.WriteLineToLog(this, "Starting " + pluginName);

		        Thread mainUIThread = new Thread(() =>
		        {
			        //InitializeErrorHandling();

			        //UNSQuestionsDialog formToShow;
			        lock (this)
			        {
						//splashScreen = new TxlSplashScreen();
						//splashScreen.Show(Screen.FromPoint(Properties.Settings.Default.WindowLocation));
						//splashScreen.Message = string.Format(
						// Properties.Resources.kstidSplashMsgRetrievingDataFromCaller, host.ApplicationName);

						//string preferredUiLocale = "en";
						//try
						//{
						// preferredUiLocale = host.GetApplicationSetting("InterfaceLanguageId");
						// if (String.IsNullOrWhiteSpace(preferredUiLocale))
						//  preferredUiLocale = "en";
						//}
						//catch (Exception)
						//{
						//}
				        const string kMajorList = "Major";
						Func<IEnumerable<IKeyTerm>> getKeyTerms = () => host.GetFactoryKeyTerms(kMajorList, "en", 01001001, 66022021);
						
						var generator = new Generator();
						if (generator.Error != null)
							MessageBox.Show(generator.Error, "Error reading file", MessageBoxButtons.OK, MessageBoxIcon.Error);
						else if (generator.CanGenerate)
				        {
					        var projectLanguageId = host.GetProjectLanguageId(projectName, "check target language");
					        var setLanguageId = (generator.TargetLanguage == projectLanguageId) ? null : projectLanguageId;
					        if (setLanguageId == null ||
						        MessageBox.Show($"Target language in file ({generator.TargetLanguage}) does not match project target language id ({projectLanguageId})." +
							        "Continue generating anyway?", "Mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
					        {
						        var englishToTermIds = new Dictionary<string, HashSet<string>>();

								foreach (var keyTerm in getKeyTerms())
								{
									HashSet<string> termIdList;
									var english = keyTerm.Term.ToLowerInvariant();
									if (!englishToTermIds.TryGetValue(english, out termIdList))
										englishToTermIds[english] = termIdList = new HashSet<string>();
									termIdList.Add(keyTerm.Id);
								}
						        Func<string, IList<string>> getRenderings = s =>
						        {
							        HashSet<string> termIdList;
							        List<string> results = new List<string>();
									if (englishToTermIds.TryGetValue(s.ToLowerInvariant(), out termIdList))
							        {
								        foreach (var termId in termIdList)
								        {
									        var renderings = host.GetProjectTermRenderings(projectName, termId, true);
									        if (renderings != null)
									        {
										        foreach (var rendering in renderings)
										        {
											        if (!results.Contains(rendering))
												        results.Add(rendering);
										        }
									        }
								        }
							        }
							        return results;
						        };

								generator.Generate(getRenderings, setLanguageId);
						        if (generator.GenerationComplete)
						        {
							        generator.Save();
							        MessageBox.Show($"Map Creator names have been set from {generator.RenderingsSet} biblical terms renderings! File saved as \n\r" +
								        $"{generator.Filename}");
						        }
					        }
				        }
						
				        //formToShow = unsMainWindow = new UNSQuestionsDialog(splashScreen, projectName,
						// () => host.GetFactoryKeyTerms(kMajorList, "en", 01001001, 66022021),
						// termId => host.GetProjectTermRenderings(projectName, termId, true),
						// host.GetProjectFont(projectName),
						// host.GetProjectLanguageId(projectName, "generate templates"),
						// host.GetProjectSetting(projectName, "Language"), host.GetProjectRtoL(projectName),
						// fileAccessor, host.GetScriptureExtractor(projectName, ExtractorType.USFX),
						// () => host.GetCssStylesheet(projectName), host.ApplicationName,
						// new ScrVers(host, TxlCore.kEnglishVersificationName),
						// new ScrVers(host, host.GetProjectVersificationName(projectName)), startRef,
						// endRef, currRef, activateKeyboard, termId => host.GetTermOccurrences(kMajorList, projectName, termId),
						// terms => host.LookUpKeyTerm(projectName, terms), fEnableDragDrop, preferredUiLocale);

						//splashScreen = null;
					}

					//#if DEBUG
					//			        // Always track if this is a debug build, but track to a different segment.io project
					//			        const bool allowTracking = true;
					//			        const string key = "0mtsix4obm";
					//#else
					//// If this is a release build, then allow an environment variable to be set to false
					//// so that testers aren't generating false analytics
					//                    string feedbackSetting = Environment.GetEnvironmentVariable("FEEDBACK");

					//                    var allowTracking = string.IsNullOrEmpty(feedbackSetting) || feedbackSetting.ToLower() == "yes" || feedbackSetting.ToLower() == "true";

					//                    const string key = "3iuv313n8t";
					//#endif
					//using (new Analytics(key, GetuserInfo(), allowTracking))
					//{
					// Analytics.Track("Startup", new Dictionary<string, string>
					//  {{"Specific version", Assembly.GetExecutingAssembly().GetName().Version.ToString()}});
					// formToShow.ShowDialog();
					//}

					ptHost.WriteLineToLog(this, "Closing " + pluginName);
			        Environment.Exit(0);
		        });
		        mainUIThread.Name = pluginName;
		        mainUIThread.IsBackground = false;
		        mainUIThread.SetApartmentState(ApartmentState.STA);
		        mainUIThread.Start();
		        // Avoid putting any code after this line. Any exceptions thrown will not be able to be reported via the
		        // "green screen" because we are not running in STA.
	        }
	        catch (Exception e)
	        {
		        MessageBox.Show("Error occurred attempting to start Transcelerator: " + e.Message);
		        throw;
	        }

		}

		public void RequestShutdown()
        {
        }

        public Dictionary<string, IPluginDataFileMergeInfo> DataFileKeySpecifications => null;

        public void Activate(string activeProjectName)
        {
        }

        /// <summary>
        /// Saves the specified text to the Paratext settings directory.
        /// </summary>
        private void SaveText(string text)
        {
            //StringWriter writer = new StringWriter();
            //data.Lines = text.Split(new [] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            //dataSerializer.Serialize(writer, data);
            //if (!host.PutPlugInData(this, projectName, savedDataId, writer.ToString()))
            //{
            //    MessageBox.Show("Unable to save the text. :(", pluginName);
            //}
        }
    }
}
