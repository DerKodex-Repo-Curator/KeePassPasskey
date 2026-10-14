// SPDX-FileCopyrightText: Copyright (C) 2026 Uwe Koegel
// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.Toolkit.Uwp.Notifications;
using KeePassPasskeyShared;
using KeePassPasskeyShared.Settings;
using KeePassPasskeyShared.Ipc;

namespace KeePassPasskeyProvider.Util;

internal static class Notifier
{
	private static bool Enabled => KeePassPasskeySettings.Current.ShowErrorNotifications;

	public static void ShowMakeCredentialError(string rpId, PipeErrorCode? code, string? errorMessage = null) =>
		ShowError("Passkey creation failed", ErrorBody(code, rpId, errorMessage));

	public static void ShowGetAssertionError(string rpId, string username, PipeErrorCode? code, string? errorMessage = null) =>
		ShowError("Sign-in failed", ErrorBody(code, rpId, errorMessage, username));

	public static void ShowPipeError(string operation) =>
		ShowError($"{operation} failed", "KeePass is not running or the database is locked.");

	public static void ShowVersionMismatch(string operation, string? appVersion, string? pluginVersion) =>
		ShowError($"{operation} failed", VersionMismatchBody(appVersion, pluginVersion));

	public static void ShowWaitingForKeePass(string operation) =>
		Show($"{operation}: can't open the KeePass unlock prompt",
			"KeePass is already showing a blocking dialog. Close it to continue.",
			TimeSpan.FromSeconds(10));

	internal static string VersionMismatchBody(string? appVersion, string? pluginVersion)
	{
		string app = PipeConstants.StripBuildMetadata(appVersion ?? "");
		string plugin = PipeConstants.StripBuildMetadata(pluginVersion ?? "");
		int cmp = PipeConstants.CompareProductVersions(appVersion, pluginVersion);
		if (cmp > 0)
			return $"The KeePass plugin ({plugin}) is older than this app ({app}). Update the KeePassPasskey plugin in KeePass.";
		if (cmp < 0)
			return $"This app ({app}) is older than the KeePass plugin ({plugin}). Update the KeePassPasskey app.";
		return "The app and the KeePass plugin have incompatible versions. Update both to the latest version.";
	}

	private static string ErrorBody(PipeErrorCode? code, string rpId, string? errorMessage, string username = "")
	{
		var detail = string.IsNullOrWhiteSpace(errorMessage) ? "" : $"\n{errorMessage}";
		string user = username.Length > 0 ? $" for {username}" : "";
		return code switch
		{
			PipeErrorCode.DbLocked => "The KeePass database is locked. Please unlock KeePass and try again.",
			PipeErrorCode.Duplicate => $"A passkey for {rpId} already exists.",
			PipeErrorCode.NotFound => $"No passkey found{user} on {rpId}.",
			PipeErrorCode.InternalError => "An internal error occurred in KeePass:" + detail,
			_ => "An unexpected error occurred:" + detail,
		};
	}

	private static void ShowError(string title, string body)
	{
		if (!Enabled) return;
		Show(title, body, TimeSpan.FromSeconds(30));
	}

	private static void Show(string title, string body, TimeSpan expiration)
	{
		try
		{
			new ToastContentBuilder()
				.AddText(title)
				.AddText(body)
				.Show(toast => toast.ExpirationTime = DateTimeOffset.Now.Add(expiration));
		}
		catch (Exception ex)
		{
			Log.Warn($"toast failed: {ex.Message}");
		}
	}
}
