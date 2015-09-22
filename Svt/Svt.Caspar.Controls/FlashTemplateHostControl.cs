using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;

using System.Text;
using System.Windows.Forms;

namespace Svt.Caspar.Controls
{
    
	public partial class FlashTemplateHostControl : UserControl
	{
	    //<invoke name=\"Add\" returntype=\"xml\"><arguments><number>") << layer << TEXT("</number><string>") << templateName << TEXT("</string>") << (playOnLoad?TEXT("<true/>"):TEXT("<false/>")) << TEXT("<string>") << label << TEXT("</string><string><![CDATA[ ") << data << TEXT(" ]]></string></arguments></invoke>");
		private const string AddRequestTemplate = "<invoke name=\"Add\" returntype=\"xml\"><arguments><number>$LAYER$</number><string>$TEMPLATE$</string><number>$MIXDURATION$</number>$PLAY$<string>$LABEL$ </string><string><![CDATA[ $DATA$ ]]></string></arguments></invoke>";
		private const string RemoveRequestTemplate = "<invoke name=\"Delete\" returntype=\"xml\"><arguments><number>$LAYER$</number></arguments></invoke>";
		private const string PlayRequestTemplate = "<invoke name=\"Play\" returntype=\"xml\"><arguments><number>$LAYER$</number></arguments></invoke>";
		private const string StopRequestTemplate = "<invoke name=\"Stop\" returntype=\"xml\"><arguments><number>$LAYER$</number><number>$MIXDURATION$</number></arguments></invoke>";
		private const string NextRequestTemplate = "<invoke name=\"Next\" returntype=\"xml\"><arguments><number>$LAYER$</number></arguments></invoke>";
		private const string UpdateRequestTemplate = "<invoke name=\"SetData\" returntype=\"xml\"><arguments><number>$LAYER$</number><string><![CDATA[ $DATA$ ]]></string></arguments></invoke>";
		private const string GotoRequestTemplate = "<invoke name=\"Goto\" returntype=\"xml\"><arguments><number>$LAYER$</number><string>$LABEL$</string></arguments></invoke>";
		private const string InvokeRequestTemplate = "<invoke name=\"ExecuteMethod\" returntype=\"xml\"><arguments><number>$LAYER$</number><string>$METHOD$</string></arguments></invoke>";

        private const string AddRequestTemplate17 = "<invoke name=\"Add\" returntype=\"xml\"><arguments><number>$LAYER$</number><string>$TEMPLATE$</string>$PLAY$<string>$LABEL$ </string><string><![CDATA[ $DATA$ ]]></string></arguments></invoke>";
        private const string RemoveRequestTemplate17 = "<invoke name=\"Delete\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array></arguments></invoke>";
		private const string PlayRequestTemplate17 = "<invoke name=\"Play\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array></arguments></invoke>";
		private const string StopRequestTemplate17 = "<invoke name=\"Stop\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array><number>$MIXDURATION$</number></arguments></invoke>";
		private const string NextRequestTemplate17 = "<invoke name=\"Next\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array></arguments></invoke>";
		private const string UpdateRequestTemplate17 = "<invoke name=\"SetData\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array><string><![CDATA[ $DATA$ ]]></string></arguments></invoke>";
		private const string GotoRequestTemplate17 = "<invoke name=\"Goto\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array><string>$LABEL$</string></arguments></invoke>";
		private const string InvokeRequestTemplate17 = "<invoke name=\"ExecuteMethod\" returntype=\"xml\"><arguments><array><property id=\"0\"><number>$LAYER$</number></property></array><string>$METHOD$</string></arguments></invoke>";

        public FlashTemplateHostControl()
		{
			InitializeComponent();

			PrepareNewFlash();

			TemplateFolder = Environment.CurrentDirectory;
            AspectControl = Aspects.Aspect169;
		}

		Svt.Caspar.Controls.ShockwaveFlashControl newFlashControl = null;
		public void PrepareNewFlash()
		{
            //this.shockwaveFlashControl1.Location = new System.Drawing.Point(25, 24);
            //this.shockwaveFlashControl1.Size = new System.Drawing.Size(265, 202);
            this.shockwaveFlashControl1.FlashActiveX.BackgroundColor = Color.FromArgb(0, bgColor).ToArgb();
            if (!string.IsNullOrEmpty(templateHost_) && System.IO.File.Exists(templateHost_))
                this.shockwaveFlashControl1.FlashActiveX.Movie = templateHost_;

            control_Resize(this, EventArgs.Empty);
		}

		private Color bgColor;
		public Color BackgroundColor    
		{
			set { bgColor = value;  shockwaveFlashControl1.FlashActiveX.BackgroundColor = Color.FromArgb(0, bgColor).ToArgb(); }
			get { return bgColor; }
		}

		string templateHost_ = string.Empty;
		[Browsable(false)]
		public string TemplateHost
		{
			get { return templateHost_; }
			set
			{
				templateHost_ = value;
				if (!string.IsNullOrEmpty(value) && System.IO.File.Exists(templateHost_))
				{
					templateFolder_ = System.IO.Path.GetDirectoryName(templateHost_);
					shockwaveFlashControl1.FlashActiveX.Movie = templateHost_;

					if(templateHost_.EndsWith("18"))
						Version = Versions.Version18;
					else if(templateHost_.EndsWith("17"))
						Version = Versions.Version17;
					else
						Version = Versions.Version16;

					Valid = true;
				}
				else
					Valid = false;
			}
		}

		string templateFolder_ = string.Empty;
		[Browsable(false)]
		public string TemplateFolder 
		{
			get
			{
				return templateFolder_;
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					try
					{
						string[] files = System.IO.Directory.GetFiles(value, "cg.fth*");
						if (files != null && files.Length > 0)
						{
							for (int i = 0; i < files.Length; ++i)
								files[i] = files[i].ToUpper();

							Array.Sort<string>(files);

							for (int i = files.Length - 1; i >= 0; --i)
							{
								if (System.IO.Path.GetFileName(files[i]).Length <= 9)
								{
									TemplateHost = files[i];
									break;
								}
							}
						}
					}
					catch { }
				}
			}
		}

		public void Clear()
		{
			PrepareNewFlash();
		}

		[Obsolete("use ICGDataContainer for CGData instead", false)]
		public bool Add(Svt.Caspar.CasparCGItem item)
		{
			if(item != null)
				return Add(item, item.Layer);
			return false;
		}

		[Obsolete("use ICGDataContainer for CGData instead", false)]
		public bool Add(Svt.Caspar.CasparCGItem item, int layer)
		{
			if (item != null)
			{
				string fullFilename = System.IO.Path.GetFullPath(System.IO.Path.Combine(TemplateFolder, item.TemplateIdentifier));
				
				if (System.IO.File.Exists(fullFilename + ".ft"))
				{
					string dataxml = Svt.Caspar.CGDataPair.ToXml(item.Data);
					string template = (Version == Versions.Version18) ? (item.TemplateIdentifier) : fullFilename + ".ft";
					StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? AddRequestTemplate17 : AddRequestTemplate, dataxml.Length + AddRequestTemplate.Length);
					request.Replace("$LAYER$", layer.ToString());
					request.Replace("$TEMPLATE$", template);
					request.Replace("$MIXDURATION$", "0");
					request.Replace("$PLAY$", "<true />");
					request.Replace("$LABEL$", string.Empty);
					request.Replace("$DATA$", dataxml);

					InvokeFlashCall(request.ToString());
					return true;
				}
			}
			return false;
		}
		public bool Add(int layer, string template)
		{
			return Add(layer, template, false, string.Empty);
		}
		public bool Add(int layer, string template, bool bPlayOnLoad)
		{
			return Add(layer, template, bPlayOnLoad, string.Empty);
		}
		public bool Add(int layer, string template, ICGDataContainer data)
		{
			return Add(layer, template, false, (data != null) ? data.ToXml() : string.Empty);
		}
		public bool Add(int layer, string template, bool bPlayOnLoad, ICGDataContainer data)
		{
			return Add(layer, template, bPlayOnLoad, (data != null) ? data.ToXml() : string.Empty);
		}
		public bool Add(int layer, string template, string data)
		{
			return Add(layer, template, false, data);
		}
		public bool Add(int layer, string template, bool bPlayOnLoad, string data)
		{
			string fullFilename = System.IO.Path.GetFullPath(System.IO.Path.Combine(TemplateFolder, template));

			if (System.IO.File.Exists(fullFilename + ".ft"))
			{
				StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? AddRequestTemplate17 : AddRequestTemplate, data.Length + AddRequestTemplate.Length);
				template = (Version == Versions.Version18) ? (template + ".ft") : fullFilename;
				request.Replace("$LAYER$", layer.ToString());
				request.Replace("$TEMPLATE$", template);
				request.Replace("$MIXDURATION$", "0");
				request.Replace("$PLAY$", "<true />");
				request.Replace("$LABEL$", string.Empty);
				request.Replace("$DATA$", data);

				InvokeFlashCall(request.ToString());
				return true;
			}
			return false;
		}

		public void Remove(int layer)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? RemoveRequestTemplate17 : RemoveRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());

			InvokeFlashCall(request.ToString());
		}

		public void Play(int layer)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? PlayRequestTemplate17 : PlayRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());

			InvokeFlashCall(request.ToString());
		}

		public void Stop(int layer)
		{
			Stop(layer, 0);
		}

		public void Stop(int layer, int mixDuration)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? StopRequestTemplate17 : StopRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());
			request.Replace("$MIXDURATION$", mixDuration.ToString());

			InvokeFlashCall(request.ToString());
		}

		public void Next(int layer)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? NextRequestTemplate17 : NextRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());

			InvokeFlashCall(request.ToString());
		}

		[Obsolete("use ICGDataContainer for CGData instead", false)]
		public void Update(Svt.Caspar.CasparCGItem item)
		{
			if (item != null)
			{
				string dataxml = Svt.Caspar.CGDataPair.ToXml(item.Data);
				StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? UpdateRequestTemplate17 : UpdateRequestTemplate, dataxml.Length + UpdateRequestTemplate.Length);
				request.Replace("$LAYER$", item.Layer.ToString());
				request.Replace("$DATA$", dataxml);

				InvokeFlashCall(request.ToString());
			}
		}

		public void Goto(int layer, string label)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? GotoRequestTemplate17 : GotoRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());
			request.Replace("$LABEL$", label);

			InvokeFlashCall(request.ToString());
		}

		public void InvokeMethod(int layer, string method)
		{
			StringBuilder request = new StringBuilder((Version == Versions.Version17 || Version == Versions.Version18) ? InvokeRequestTemplate17 : InvokeRequestTemplate);
			request.Replace("$LAYER$", layer.ToString());
			request.Replace("$METHOD$", method);

			InvokeFlashCall(request.ToString());
		}

		public bool Valid { get; set; }

		private void InvokeFlashCall(string request)
		{
			try
			{
				if(Valid)
					shockwaveFlashControl1.FlashActiveX.CallFunction(request);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		public Versions Version
		{
			get;
			set;
		}

		public static string GetVersionString(Versions v)
		{
			if(v == Versions.Version16) return "v1.6";
			if(v == Versions.Version17) return "v1.7";
			if(v == Versions.Version18) return "v1.8";
			return "unknown";
		}

		public enum Versions
		{
			Version16 = 0,
			Version17,
			Version18
		}

        public enum ScaleModes
        {
            FullScreen,
            Unknown1,
            Fit
        }

		public ScaleModes ScaleMode
        {
            set
            {
				shockwaveFlashControl1.FlashActiveX.ScaleMode = (int)value;
            }

            get
            {
				return (ScaleModes)shockwaveFlashControl1.FlashActiveX.ScaleMode;
            }
        }

        public enum Aspects
        {
            None,
            Aspect169,
            Aspect43
        }

		Aspects aspect;
        public Aspects AspectControl 
		{
			get { return aspect; }
			set
			{
				aspect = value;
				control_Resize(this, EventArgs.Empty);
			}
		}

		public Control FlashControl
		{
			get { return shockwaveFlashControl1; }
		}
		#region Aspectcontrol

		private void control_Resize(object sender, EventArgs e)
		{
			Control container = (Control)sender;
			Size bounds = container.Size;

			Size finalSize = new Size(0, 0);

			//Calculate largest aspect-correct rect for the flashcontrol
            if (AspectControl == Aspects.Aspect169)
                finalSize = GetLargestAspectCorrectRect(bounds, new Size(16, 9));
            else if (AspectControl == Aspects.Aspect43)
                finalSize = GetLargestAspectCorrectRect(bounds, new Size(4, 3));
            else
                finalSize = bounds;

			//Position the control
			Point position = new Point();
			position.X = bounds.Width / 2 - finalSize.Width / 2;
			position.Y = bounds.Height / 2 - finalSize.Height / 2;

			shockwaveFlashControl1.Size = finalSize;
			shockwaveFlashControl1.Location = position;
		}

		private Size GetLargestAspectCorrectRect(Size bounds, Size aspect)
		{
			Size finalSize = new Size();
			if (bounds.Width / aspect.Width < bounds.Height / aspect.Height)
			{
				finalSize.Width = (bounds.Width / aspect.Width) * aspect.Width;
				finalSize.Height = (bounds.Width / aspect.Width) * aspect.Height;
			}
			else
			{
				finalSize.Height = (bounds.Height / aspect.Height) * aspect.Height;
				finalSize.Width = (bounds.Height / aspect.Height) * aspect.Width;
			}

			return finalSize;
		}
		#endregion

	}
}
