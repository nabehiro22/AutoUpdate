using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace AutoUpdate
{
	public class ApplicationUpdate
	{
		/// <summary>
		/// アップデート確認
		/// </summary>
		/// <param name="path">最新ファイルが存在するパス</param>
		/// <param name="extension">チェックする拡張子(拡張子が限定されない場合はnull)</param>
		/// <param name="holding">除外するファイル(不要な場合はnull)</param>
		/// <returns>false=失敗 true=成功</returns>
		public bool Update(string path, List<string> extension = null, List<string> holding = null)
		{
			// 戻り値
			bool result = true;
			// 更新判定
			bool isUpdate = false;
			try
			{
				// 論理ドライブの一覧を取得する
				List<string> drive = new(Directory.GetLogicalDrives());
				// 論理ドライブに引数で指定されたドライブが存在すれば処理を続行(ネットワークドライブ未接続対策)
				if (drive.Any(d => d == Path.GetPathRoot(path)) == true)
				{
					List<string> newFile = new();
					List<string> myFile = new();
					// 更新するファイルの拡張子指定有無で取得方法を分ける
					if (extension == null)
					{
						newFile.AddRange(Directory.GetFiles(path, "*.*").Select(f => Path.GetFileName(f)).ToList());
						myFile.AddRange(Directory.GetFiles(Environment.CurrentDirectory, "*.*").Select(f => Path.GetFileName(f)).ToList());
					}
					else
					{
						foreach (var ext in extension)
						{
							newFile.AddRange(Directory.GetFiles(path, $"*.{ext}").Select(f => Path.GetFileName(f)).ToList());
							myFile.AddRange(Directory.GetFiles(Environment.CurrentDirectory, $"*.{ext}").Select(f => Path.GetFileName(f)).ToList());
						}
					}
					// どちらのフォルダーにも存在して更新するかもしれないファイルは比較して相違があればコピー
					foreach (var f in myFile.Intersect(newFile))
					{
						// コピーする条件はFileVersionの相違または日付の相違
						if ((FileVersionInfo.GetVersionInfo($@"{path}\{f}").FileVersion != FileVersionInfo.GetVersionInfo($@"{Environment.CurrentDirectory}\{f}").FileVersion) ||
							(File.GetLastWriteTime($@"{path}\{f}") != File.GetLastWriteTime($@"{Environment.CurrentDirectory}\{f}")))
						{
							isUpdate = true;
							// 古いファイルは異常発生時のリカバリのため今は拡張子を変えて残しておく
							File.Move($@"{Environment.CurrentDirectory}\{f}", $@"{Environment.CurrentDirectory}\{f}.delete");
							// 新しいファイルをコピー
							File.Copy($@"{path}\{f}", $@"{Environment.CurrentDirectory}\{f}", true);
						}
					}
					// 自分の運用フォルダには存在しないが更新ファイルには存在する一覧(無条件コピー)
					foreach (var f in newFile.Except(myFile))
					{
						isUpdate = true;
						File.Copy($@"{path}\{f}", $@"{Environment.CurrentDirectory}\{f}");
					}
					// 自分の運用フォルダには存在するが更新ファイルには存在しない一覧(無条件削除)
					if (holding == null)
					{
						foreach (var f in myFile.Except(newFile))
						{
							isUpdate = true;
							File.Delete($@"{Environment.CurrentDirectory}\{f}");
						}
					}
					else
					{
						foreach (var f in myFile.Except(holding).Except(newFile))
						{
							isUpdate = true;
							File.Delete($@"{Environment.CurrentDirectory}\{f}");
						}
					}
				}
			}
			catch (Exception)
			{
				/***** 異常が発生したら.deleteファイルを復旧させる *****/
				// 拡張子が.deleteの一覧取得
				List<string> restoreFiles = new(Directory.GetFiles(Environment.CurrentDirectory, "*.delete"));
				// 上記一覧から復旧させるファイル名を生成(「.delete」を除いたファイル名)
				List<string> recoveryFiles = restoreFiles.Select(r => r.Replace(".delete", "")).ToList();
				// ファイルを復旧させる
				for (int i = 0; i < restoreFiles.Count; i++)
				{
					// 更新したファイルは削除
					File.Delete(recoveryFiles[i]);
					// 拡張子を「.delete」としたファイルは元の名称へ
					File.Move(restoreFiles[i], recoveryFiles[i]);
				}
				result = false;
				_ = MessageBox.Show("アップデートで問題が発生しました。\r\n以前のバージョンに戻します。", $"{Assembly.GetExecutingAssembly().GetName().Name} アップデート", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				// 更新が発生していたら再起動
				if (result == true && isUpdate == true)
				{
					_ = MessageBox.Show("新しいバージョンのアプリケーションがありますので更新します。\r\n更新後は自動的に再起動しますのでしばらくお待ちください。", $"{Assembly.GetExecutingAssembly().GetName().Name} 更新", MessageBoxButton.OK, MessageBoxImage.Information);
					_ = Process.Start(Process.GetCurrentProcess().MainModule.FileName, $"/up {Environment.ProcessId}");
					Application.Current.Shutdown();
				}
			}
			return result;
		}
	}
}
