package com.matias.hillhistory;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import android.provider.DocumentsContract;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.security.MessageDigest;

/** Public history only. No storage permissions, profile import or network APIs. */
public final class HistoryExportActivity extends Activity {
    private static final int CREATE_ZIP = 7181;
    public static void begin(Activity caller, String source, String name) {
        Intent intent = new Intent(caller, HistoryExportActivity.class);
        intent.putExtra("historySource", source);
        intent.putExtra("historyName", name);
        caller.startActivity(intent);
    }
    @Override protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        if (saved != null) return;
        Intent picker = new Intent(Intent.ACTION_CREATE_DOCUMENT);
        picker.addCategory(Intent.CATEGORY_OPENABLE);
        picker.setType("application/zip");
        picker.putExtra(Intent.EXTRA_LOCAL_ONLY, true);
        picker.putExtra(Intent.EXTRA_TITLE, getIntent().getStringExtra("historyName"));
        try { startActivityForResult(picker, CREATE_ZIP); }
        catch (Exception failure) { finishWith("failed"); }
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != CREATE_ZIP) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { finishWith("cancelled"); return; }
        final Uri uri = data.getData();
        final String source = getIntent().getStringExtra("historySource");
        new Thread(() -> {
            String status = "failed";
            try {
                File file = new File(source).getCanonicalFile();
                File cache = getCacheDir().getCanonicalFile();
                File externalCache = getExternalCacheDir();
                boolean inCache = file.getPath().startsWith(cache.getPath() + File.separator) ||
                    (externalCache != null && file.getPath().startsWith(externalCache.getCanonicalPath() + File.separator));
                // The bridge accepts only this app's staged, verified history ZIP.
                if (!inCache || !file.getName().startsWith("hill-season-") || !file.getName().endsWith(".zip"))
                    throw new SecurityException("Invalid history staging path");
                byte[] expected;
                try (InputStream input = new FileInputStream(file)) { expected = digest(input); }
                try (ParcelFileDescriptor descriptor = getContentResolver().openFileDescriptor(uri, "rwt")) {
                    if (descriptor == null) throw new java.io.IOException("No document descriptor");
                    try (FileInputStream input = new FileInputStream(file);
                         OutputStream output = new ParcelFileDescriptor.AutoCloseOutputStream(descriptor)) {
                        byte[] buffer = new byte[32768];
                        for (int count; (count = input.read(buffer)) >= 0;) if (count > 0) output.write(buffer, 0, count);
                        output.flush(); descriptor.getFileDescriptor().sync();
                    }
                }
                try (InputStream input = getContentResolver().openInputStream(uri)) {
                    if (input == null || !MessageDigest.isEqual(expected, digest(input))) throw new java.io.IOException("Document verification failed");
                }
                status = "saved";
            } catch (Exception failure) {
                // Only the just-created document may be removed on failure.
                try { DocumentsContract.deleteDocument(getContentResolver(), uri); } catch (Exception ignored) { }
            }
            final String completed = status;
            runOnUiThread(() -> finishWith(completed));
        }, "HillSeasonHistorySave").start();
    }
    private static byte[] digest(InputStream input) throws Exception {
        MessageDigest hash = MessageDigest.getInstance("SHA-256"); byte[] buffer = new byte[32768];
        for (int count; (count = input.read(buffer)) >= 0;) if (count > 0) hash.update(buffer, 0, count);
        return hash.digest();
    }
    private void finishWith(String result) {
        UnityPlayer.UnitySendMessage("HillSeasonHistoryExport", "OnDocumentSaved", result);
        finish();
    }
}
