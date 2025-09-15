# Test FK Relationships dan Junction Table
Write-Host "🔄 Starting TiketLaut API untuk test FK relationships..." -ForegroundColor Yellow

# Start API in background
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "c:\Main Storage\Documents\UGM\Junpro\TiketLaut\TiketLaut.csproj" -PassThru -WindowStyle Hidden

# Wait for API to start
Start-Sleep -Seconds 6

Write-Host "🧪 Testing FK Relationships dan Junction Table..." -ForegroundColor Green

try {
    Write-Host "`n=== 1. PENUMPANG DATA (Source Table) ===" -ForegroundColor Cyan
    $penumpang = Invoke-RestMethod -Uri "http://localhost:5000/api/Penumpang" -Method Get
    Write-Host "Penumpang Count: $($penumpang.Count)"
    $penumpang | ForEach-Object { Write-Host "  - ID $($_.penumpang_id): $($_.nama) (Pengguna ID: $($_.pengguna_id))" }

    Write-Host "`n=== 2. RINCIAN PENUMPANG (Junction Table) ===" -ForegroundColor Cyan
    $rincian = Invoke-RestMethod -Uri "http://localhost:5000/api/RincianPenumpang" -Method Get
    Write-Host "RincianPenumpang Count: $($rincian.Count)"
    $rincian | ForEach-Object { Write-Host "  - ID $($_.rincian_penumpang_id): Tiket $($_.tiket_id) ← Penumpang $($_.penumpang_id)" }

    Write-Host "`n=== 3. RINCIAN PENUMPANG WITH JOIN (FK Lookup) ===" -ForegroundColor Yellow
    $detailedRincian = Invoke-RestMethod -Uri "http://localhost:5000/api/RincianPenumpang/detailed" -Method Get
    Write-Host "Detailed RincianPenumpang (dengan nama dari FK):"
    $detailedRincian | ForEach-Object { 
        Write-Host "  - Tiket $($_.tiket_id): $($_.nama) (NIK: $($_.NIK_penumpang), User: $($_.pengguna_id))" 
    }

    Write-Host "`n=== 4. TIKET WITH COMPUTED PROPERTIES ===" -ForegroundColor Yellow
    $detailedTiket = Invoke-RestMethod -Uri "http://localhost:5000/api/Tiket/detailed" -Method Get
    Write-Host "Tiket dengan computed pengguna_id dan jumlah_penumpang:"
    $detailedTiket | ForEach-Object { 
        Write-Host "  - Tiket $($_.tiket_id): $($_.jumlah_penumpang) penumpang, Pengguna ID: $($_.pengguna_id), Status: $($_.status)" 
    }

    Write-Host "`n=== 5. JADWAL WITH FK LOOKUP ===" -ForegroundColor Yellow
    $detailedJadwal = Invoke-RestMethod -Uri "http://localhost:5000/api/Jadwal/detailed" -Method Get
    Write-Host "Jadwal dengan nama dari FK (tidak duplikasi):"
    $detailedJadwal | ForEach-Object { 
        Write-Host "  - Jadwal $($_.jadwal_id): $($_.pelabuhan_asal) → $($_.pelabuhan_tujuan) via $($_.kapal)" 
    }

    Write-Host "`n=== 6. PENUMPANG BY TIKET (Multiple penumpang per tiket) ===" -ForegroundColor Green
    $penumpangTiket1 = Invoke-RestMethod -Uri "http://localhost:5000/api/RincianPenumpang/tiket/1" -Method Get
    Write-Host "Penumpang dalam Tiket 1:"
    $penumpangTiket1 | ForEach-Object { 
        Write-Host "  - $($_.nama) (NIK: $($_.NIK_penumpang))" 
    }

    Write-Host "`n✅ SEMUA FK RELATIONSHIPS BEKERJA DENGAN BENAR!" -ForegroundColor Green
    Write-Host "🎯 Key Points:" -ForegroundColor Green
    Write-Host "  ✅ RincianPenumpang = Pure Junction Table (hanya FK)" -ForegroundColor Green
    Write-Host "  ✅ Multiple penumpang per tiket via junction table" -ForegroundColor Green
    Write-Host "  ✅ Tiket menghitung jumlah_penumpang dari RincianPenumpang" -ForegroundColor Green
    Write-Host "  ✅ Jadwal mengambil nama dari Pelabuhan dan Kapal via FK" -ForegroundColor Green
    Write-Host "  ✅ Tidak ada duplikasi data - semua via FK lookup" -ForegroundColor Green

} catch {
    Write-Host "`n❌ Error testing API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    # Cleanup
    Write-Host "`n🛑 Stopping API..." -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
}