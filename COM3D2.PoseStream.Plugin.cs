using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityInjector;
using UnityInjector.Attributes;

namespace COM3D2.PoseStream.Plugin
{
	#region PluginMain
	///=========================================================================
	/// <summary>動くモーション作成プラグイン</summary>
	/// <remarks>
	///	COM3D2.PoseStream.Plugin : 複数のポーズをつなぎ合わせて一つのanmファイルを生成する UnityInjector/Sybaris 用クラス
	///
	/// </remarks>
	///=========================================================================
	[PluginFilter( "COM3D2x64" ), PluginName("COM3D2.PoseStream.Plugin"), PluginVersion( "1.4.0.0" )]
	public class PoseStream : UnityInjector.PluginBase
	{
		private bool isStudio = false;
		private bool isGUI = false;
		private String anmName = "";
		private String resultMessage = "";

		public void Awake()
		{
			try
			{
				GameObject.DontDestroyOnLoad(this);
				SceneManager.sceneLoaded += OnSceneLoaded;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		public void Update()
		{
			try
			{
				if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
					(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
					Input.GetKeyDown(KeyCode.M))
				{
					isGUI = !isGUI;
					resultMessage = "";
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private String getPoseDataPath(bool b = false)
		{
			String path = Application.dataPath;
			path = path.Substring(0, path.Length - @"COM3D2x64_Data".Length);
			if (isStudio)
			{
				path += @"PhotoModeData\MyPose";
			}
			else
			{
				path += @"Mod\MultipleMaidsPose";
			}
			if (b)
			{
				path += @"\";
			}
			return path;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
		{
			try
			{
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private enum GUIPARAM
		{
			S, W, H, X, Y, EXEBW, EXEBH, EXEBX, EXEBY, TW, TH, TX, TY, LW, LH, LX, LY, RW, RH, RX, RY, XW, XH, XX, XY, EXEBY2
		}

		private int getGUIparam(GUIPARAM p)
		{
			switch (p)
			{
				case GUIPARAM.S:
					return 16;
				case GUIPARAM.W:
					return Screen.width / 3;
				case GUIPARAM.H:
					return Screen.height / 4;
				case GUIPARAM.X:
					return (Screen.width - getGUIparam(GUIPARAM.W)) / 2;
				case GUIPARAM.Y:
					return (Screen.height - getGUIparam(GUIPARAM.H)) / 2;
				case GUIPARAM.EXEBW:
					return getGUIparam(GUIPARAM.W) / 5;
				case GUIPARAM.EXEBH:
					return getGUIparam(GUIPARAM.H) / 5;
				case GUIPARAM.EXEBX:
					return getGUIparam(GUIPARAM.W) / 40;
				case GUIPARAM.EXEBY:
					return getGUIparam(GUIPARAM.H) * 5 / 10;
				case GUIPARAM.EXEBY2:
					return getGUIparam(GUIPARAM.H) * 7 / 10;
				case GUIPARAM.TW:
					return getGUIparam(GUIPARAM.W) * 4 / 5;
				case GUIPARAM.TH:
					return getGUIparam(GUIPARAM.H) / 5;
				case GUIPARAM.TX:
					return getGUIparam(GUIPARAM.W) / 40;
				case GUIPARAM.TY:
					return getGUIparam(GUIPARAM.H) * 1 / 10;
				case GUIPARAM.LW:
					return getGUIparam(GUIPARAM.W) * 4 / 5;
				case GUIPARAM.LH:
					return getGUIparam(GUIPARAM.H) / 5;
				case GUIPARAM.LX:
					return getGUIparam(GUIPARAM.TX);
				case GUIPARAM.LY:
					return getGUIparam(GUIPARAM.TY) + getGUIparam(GUIPARAM.TH);
				case GUIPARAM.RW:
					return getGUIparam(GUIPARAM.W) - getGUIparam(GUIPARAM.EXEBX) + getGUIparam(GUIPARAM.EXEBW);
				case GUIPARAM.RH:
					return getGUIparam(GUIPARAM.H) * 2 / 5;
				case GUIPARAM.RX:
					return getGUIparam(GUIPARAM.EXEBX) + getGUIparam(GUIPARAM.EXEBW);
				case GUIPARAM.RY:
					return getGUIparam(GUIPARAM.EXEBY);
				case GUIPARAM.XW:
					return getGUIparam(GUIPARAM.W) / 15;
				case GUIPARAM.XH:
					return getGUIparam(GUIPARAM.W) / 15;
				case GUIPARAM.XX:
					return getGUIparam(GUIPARAM.W) * 14 / 15;
				case GUIPARAM.XY:
					return 0;
				default:
					return 0;
			}
		}

		private String anmMake()
		{
			if (anmName.Length == 0)
			{
				return myError("名前が入力されていません");
			}
			String[] files = Directory.GetFiles(getPoseDataPath(), anmName + @"_????????.anm", SearchOption.TopDirectoryOnly);
			if (files == null || files.Length == 0)
			{
				return myError("連結対象のポーズ(" + anmName + "_00000000～)がありません");
			}
			List<String> lf = new List<string>(files);
			List<String> ps = new List<string>();
			foreach (String s in lf)
			{
				String s2 = s.Substring(getPoseDataPath(true).Length);
				if (s2.Length != (anmName.Length + @"_00000000.anm".Length))
				{
					//8桁でない
					continue;
				}
				if (get00000000byInt(s2) < 0)
				{
					//自然数でない
					continue;
				}
				ps.Add(s2);
			}
			
			if (ps.Count == 0)
			{
				return myError("連結対象のポーズ(" + anmName + "_00000000～)が見つかりません");
			}
			ps.Sort();
			if (ps.Count == 1)
			{
				return myError("連結対象のポーズ(" + anmName + "_00000000～)が一つしかありません");
			}
			else if (get00000000byInt(ps[0]) != 0)
			{
				return myError("最初のポーズ(" + anmName + "_00000000)がありません");
			}

			if (makeAnmFile(ps.ToArray()))
			{
				if (isStudio)
				{
					return "モーション「" + anmName + ".anm」を生成しました\nマイポーズのカテゴリをすでに表示している場合は\n一旦別のカテゴリを表示後にマイポーズを再表示してください";
				}
				else
				{
					return "モーション「" + anmName + ".anm」を生成しました\nポーズ一覧に今すぐ反映したい場合は\n別の名前でポーズを登録するとリストに出現します";
				}
			}
			return myError(errorFile);
		}

		public String errorFile = "";

		private bool makeAnmFile(String[] ss)
		{
			List<BoneDataA> bda = new List<BoneDataA>();
			byte[] header = new byte[15];
			bool isFirst = true;
			//読み込み
			foreach (String s in ss)
			{
				using (BinaryReader r = new BinaryReader(File.OpenRead(getPoseDataPath(true) + s)))
				{
					try
					{
						if (isFirst)
						{
							isFirst = false;
							//最初のファイルでヘッダー部分を決定
							for (int i = 0; i < 15; i++)
							{
								header[i] = r.ReadByte();
							}
							byte t = r.ReadByte();
							while (true)
							{
								if (t == 1)
								{
									BoneDataA a = new BoneDataA();
									//A先頭部分
									t = r.ReadByte();
									byte c = 0;
									c = r.ReadByte();
									if (c == 1)
									{
										a.isB = true;
										a.name = "";
									}
									else
									{
										a.isB = false;
										a.name = "" + (char)c;
										t--;
									}
									for (int i = 0; i < t; i++)
									{
										c = r.ReadByte();
										a.name += (char)c;
									}
									a.b = new List<BoneDataB>();
									//B部分
									while(true)
									{
										t = r.ReadByte();
										if (t >= 64)
										{
											BoneDataB b = new BoneDataB();
											b.index = t;
											int tmpf = r.ReadByte();
											r.ReadByte();
											r.ReadByte();
											r.ReadByte();
											//C部分
											bool firstFrame = true;
											for (int i = 0; i < tmpf; i++)
											{
												if (firstFrame)
												{
													firstFrame = false;
													b.c = new List<BoneDataC>();
													BoneDataC bc = new BoneDataC();
													bc.time = 0;
													//time
													r.ReadBytes(4);
													//raw
													bc.raw = r.ReadBytes(12);
													b.c.Add(bc);
												}
												else
												{
													r.ReadBytes(16);
												}
											}
											a.b.Add(b);
										}
										else
										{
											break;
										}
									}

									bda.Add(a);
								}
								else
								{
									break;
								}
							}
						}
						else
						{
							int time = get00000000byInt(s);
							//最初以外はヘッダー部分を読み飛ばす
							r.ReadBytes(15);
							byte t = r.ReadByte();
							while (true)
							{
								if (t == 1)
								{
									//A先頭部分
									t = r.ReadByte();
									byte c = 0;
									String name;
									bool isB = false;
									c = r.ReadByte();
									if (c == 1)
									{
										isB = true;
										name = "";
									}
									else
									{
										isB = false;
										name = "" + (char)c;
										t--;
									}
									for (int i = 0; i < t; i++)
									{
										c = r.ReadByte();
										name += (char)c;
									}
									BoneDataA a = null;
									foreach (BoneDataA tmp in bda)
									{
										if (tmp.name.Equals(name))
										{
											a = tmp;
											break;
										}
									}
									if (a == null)
									{
										a = new BoneDataA();
										a.name = name;
										a.isB = isB;
										a.b = new List<BoneDataB>();
										//B部分
										while (true)
										{
											t = r.ReadByte();
											if (t >= 64)
											{
												BoneDataB b = new BoneDataB();
												b.index = t;
												int tmpf = r.ReadByte();
												r.ReadByte();
												r.ReadByte();
												r.ReadByte();
												//C部分
												bool firstFrame = true;
												for (int i = 0; i < tmpf; i++)
												{
													if (firstFrame)
													{
														firstFrame = false;
														b.c = new List<BoneDataC>();
														BoneDataC bc = new BoneDataC();
														bc.time = 0;
														//time
														r.ReadBytes(4);
														//raw
														bc.raw = r.ReadBytes(12);
														b.c.Add(bc);
													}
													else
													{
														BoneDataC bc = new BoneDataC();
														bc.time = time;
														//time
														r.ReadBytes(4);
														//raw
														bc.raw = r.ReadBytes(12);
														b.c.Add(bc);
													}
												}
												a.b.Add(b);
											}
											else
											{
												break;
											}
										}
										bda.Add(a);
									}
									else
									{
										//B部分
										while (true)
										{
											t = r.ReadByte();
											if (t >= 64)
											{
												BoneDataB b = null;
												foreach (BoneDataB tmpb in a.b)
												{
													if (t == tmpb.index)
													{
														b = tmpb;
														break;
													}
												}
												if (b == null)
												{
													b = new BoneDataB();
													b.index = t;
													int tmpf = r.ReadByte();
													r.ReadByte();
													r.ReadByte();
													r.ReadByte();
													//C部分
													bool firstFrame = true;
													for (int i = 0; i < tmpf; i++)
													{
														if (firstFrame)
														{
															firstFrame = false;
															b.c = new List<BoneDataC>();
															BoneDataC bc = new BoneDataC();
															bc.time = 0;
															//time
															r.ReadBytes(4);
															//raw
															bc.raw = r.ReadBytes(12);
															b.c.Add(bc);
														}
														else
														{
															BoneDataC bc = new BoneDataC();
															bc.time = time;
															//time
															r.ReadBytes(4);
															//raw
															bc.raw = r.ReadBytes(12);
															b.c.Add(bc);
														}
													}
													a.b.Add(b);
												}
												else
												{
													int tmpf = r.ReadByte();
													r.ReadByte();
													r.ReadByte();
													r.ReadByte();
													//C部分
													bool firstFrame = true;
													for (int i = 0; i < tmpf; i++)
													{
														if (firstFrame)
														{
															firstFrame = false;
															BoneDataC bc = new BoneDataC();
															bc.time = time;
															//time
															r.ReadBytes(4);
															//raw
															bc.raw = r.ReadBytes(12);
															b.c.Add(bc);
														}
														else
														{
															r.ReadBytes(16);
														}
													}
												}
											}
											else
											{
												break;
											}
										}
									}

								}
								else
								{
									break;
								}
							}
						}
					}
					catch (Exception)
					{
						errorFile = "ポーズ「" + s + "」の読み込み中にエラーが発生しました";
						return false;
					}
				}
			}

			//結合
			bool isExist = File.Exists(getPoseDataPath(true) + anmName + ".anm");
			using (BinaryWriter w = new BinaryWriter(File.Create(getPoseDataPath(true) + anmName + ".anm")))
			{
				try
				{
					w.Write(header);
					foreach (BoneDataA a in bda)
					{
						w.Write(a.outputABinary());
					}
					w.Write((byte)0);
					w.Write((byte)0);
					w.Write((byte)0);
				}
				catch (Exception)
				{
					errorFile = "モーション「" + anmName + ".anm」の書き出し中にエラーが発生しました";
					return false;
				}
			}
			if (!isExist)
			{
				MotionWindow mw = GameObject.FindObjectOfType<MotionWindow>();
				if (mw != null)
				{
					PopupAndTabList patl = mw.PopupAndTabList;
					try
					{
						mw.AddMyPose(getPoseDataPath(true) + anmName + @".anm");
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
				}
			}

			return true;
		}

		private class BoneDataA
		{
			public String name = "";
			public bool isB = false;
			public List<BoneDataB> b = null;
			public byte[] outputABinary()
			{
				List<byte> lb = new List<byte>();
				//01
				lb.Add(1);
				//(ボーン名の長さ)
				lb.Add((byte)(name.Length));
				//[01](長いボーン名だとある？)
				if (isB)
				{
					lb.Add(1);
				}
				//ボーン名
				char[] c = name.ToCharArray();
				for (int i = 0; i < name.Length; i++)
				{
					lb.Add((byte)(c[i]));
				}
				//B
				foreach(BoneDataB b1 in b)
				{
					lb.AddRange(b1.outputBBinary());
				}
				return lb.ToArray();
			}
		}
		private class BoneDataB
		{
			public int index = 0;
			public List<BoneDataC> c = null;
			public byte[] outputBBinary()
			{
				List<byte> lb = new List<byte>();
				//XX(64から始まる)
				lb.Add((byte)index);
				//XX XX XX XX(フレーム数)
				lb.Add((byte)((c.Count) % 256));
				lb.Add((byte)(((c.Count) / 256) % 256));
				lb.Add((byte)((((c.Count) / 256) / 256) % 256));
				lb.Add((byte)(((((c.Count) / 256) / 256) / 256) % 256));
				//C
				foreach (BoneDataC c1 in c)
				{
					lb.AddRange(c1.outputCBinary());
				}
				return lb.ToArray();
			}
		}
		private class BoneDataC
		{
			public int time = 0;
			public byte[] raw = null;
			public byte[] outputCBinary()
			{
				List<byte> lb = new List<byte>();
				float f = time / 1000f;
				byte[] b = new byte[4];
				byte[] t = BitConverter.GetBytes(f);
				b[0] = t[0];
				b[1] = t[1];
				b[2] = t[2];
				b[3] = t[3];
				//時間
				lb.AddRange(b);
				//データ
				lb.AddRange(raw);
				return lb.ToArray();
			}
		}

		private int get00000000byInt(String s)
		{
			String cut = s.Substring(anmName.Length + 1, 8);
			int t = 0;
			try
			{
				for (int i = 0; i < 8; i++)
				{
					t = t * 10 + int.Parse(cut.Substring(i, 1));
				}
			}
			catch (Exception)
			{
				t = -1;
			}
			return t;
		}

		private String myError(String s)
		{
			return "エラー:" + s;
		}

		private void onGUIFunc(int winId)
		{
			GUIStyle gsTextFiled = new GUIStyle(GUI.skin.textField);
			gsTextFiled.fontSize = getGUIparam(GUIPARAM.S);
			anmName = GUI.TextField(new Rect(getGUIparam(GUIPARAM.TX), getGUIparam(GUIPARAM.TY), getGUIparam(GUIPARAM.TW), getGUIparam(GUIPARAM.TH)), anmName, 30, gsTextFiled);
			GUIStyle gsLabel = new GUIStyle(GUI.skin.label);
			gsLabel.fontSize = getGUIparam(GUIPARAM.S);
			GUI.Label(new Rect(getGUIparam(GUIPARAM.LX), getGUIparam(GUIPARAM.LY), getGUIparam(GUIPARAM.LW), getGUIparam(GUIPARAM.LH)), "名前（*_00000000の*の部分）を入力してください\nすでに生成先ファイルが存在する場合は上書きされます", gsLabel);
			GUIStyle gsButton2 = new GUIStyle(GUI.skin.button);
			gsButton2.fontSize = getGUIparam(GUIPARAM.S) * 2;
			gsButton2.alignment = TextAnchor.MiddleCenter;
			if (GUI.Button(new Rect(getGUIparam(GUIPARAM.XX), getGUIparam(GUIPARAM.XY), getGUIparam(GUIPARAM.XW), getGUIparam(GUIPARAM.XH)), "×", gsButton2))
			{
				isGUI = false;
				resultMessage = "";
			}
			GUIStyle gsButton = new GUIStyle(GUI.skin.button);
			gsButton.fontSize = getGUIparam(GUIPARAM.S);
			gsButton.alignment = TextAnchor.MiddleLeft;
			gsButton.wordWrap = true;
			if (GUI.Button(new Rect(getGUIparam(GUIPARAM.EXEBX), getGUIparam(GUIPARAM.EXEBY), getGUIparam(GUIPARAM.EXEBW), getGUIparam(GUIPARAM.EXEBH)), "マイポーズで生成", gsButton))
			{
				isStudio = true;
				resultMessage = anmMake();
			}
			if (GUI.Button(new Rect(getGUIparam(GUIPARAM.EXEBX), getGUIparam(GUIPARAM.EXEBY2), getGUIparam(GUIPARAM.EXEBW), getGUIparam(GUIPARAM.EXEBH)), "複数メイド撮影ポーズで生成", gsButton))
			{
				isStudio = false;
				resultMessage = anmMake();
			}
			GUI.Label(new Rect(getGUIparam(GUIPARAM.RX), getGUIparam(GUIPARAM.RY), getGUIparam(GUIPARAM.RW), getGUIparam(GUIPARAM.RH)), resultMessage, gsLabel);
		}

		public void OnGUI()
		{
			try
			{
				if (isGUI)
				{
					GUIStyle gsWin = new GUIStyle(GUI.skin.box);
					gsWin.fontSize = getGUIparam(GUIPARAM.S);
					gsWin.alignment = TextAnchor.UpperLeft;
					GUI.Window(200, new Rect(getGUIparam(GUIPARAM.X), getGUIparam(GUIPARAM.Y), getGUIparam(GUIPARAM.W), getGUIparam(GUIPARAM.H)), onGUIFunc, "PoseStream 1.4.0.0", gsWin);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private void Initialize()
		{
			try
			{
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}
		
		
	}
	#endregion
}