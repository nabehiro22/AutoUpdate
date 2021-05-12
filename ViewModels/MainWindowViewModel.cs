using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;

namespace AutoUpdate.ViewModels
{
	public class MainWindowViewModel : BindableBase, INotifyPropertyChanged
	{
		/// <summary>
		/// タイトル
		/// </summary>
		public static ReactivePropertySlim<string> Title { get; } = new("AutoUpdate");

		/// <summary>
		/// 最初にウインドウを表示させた時
		/// </summary>
		public static ReactiveCommand ContentRenderedCommand { get; } = new();

		/// <summary>
		/// ウインドウを閉じる時
		/// </summary>
		public ReactiveCommand ClosedCommand { get; } = new();

		/// <summary>
		/// Disposeが必要な処理をまとめてやる
		/// </summary>
		private CompositeDisposable Disposable { get; } = new();

		/// <summary>
		/// コマンドライン引数
		/// </summary>
		private readonly List<string> args = new(Environment.GetCommandLineArgs());

		/// <summary>
		///	コンストラクタ
		/// </summary>
		public MainWindowViewModel()
		{
			// ウインドウが閉じる時の処理
			_ = ClosedCommand.Subscribe(Close).AddTo(Disposable);
			// ウインドウが最初に表示された時
			_ = ContentRenderedCommand.Subscribe(Update).AddTo(Disposable);
			// 再起動かを判別するためタイトルを変えてみる
			if (args.Any(a => a == "/up") == true)
			{
				Title.Value += " 更新完了";
			}
		}

		/// <summary>
		/// アプリが閉じられる時
		/// </summary>
		private void Close()
		{
			Disposable.Dispose();
		}

		/// <summary>
		/// 自動更新チェック
		/// コマンドライン引数に「/UP」がなければ通常起動なのでアップデートチェック
		/// コマンドライン引数に「/UP」があれば更新後なので不要なファイルを削除
		/// </summary>
		private void Update()
		{
			if (args.Any(a => a == "/up") == false)
			{
				// 今回は実験なので拡張子がtxtのだけ更新
				List<string> extension = new() { "txt" };
				ApplicationUpdate update = new();
				if (update.Update(@"D:\Files", extension) == true)
				{
					Title.Value += " 更新チェック完了 更新なし";
				}

			}
			else
			{
				// コマンドライン引数に「/up」があれば更新処理があったので拡張子が「delete」の古いファイルを取得し削除
				foreach (var f in Directory.GetFiles(Environment.CurrentDirectory, "*.delete"))
				{
					File.Delete(f);
				}
			}
		}
	}
}
