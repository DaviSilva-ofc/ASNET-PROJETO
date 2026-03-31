
$file1 = 'c:\Users\Estagio\Documents\GitHub\ASNET-PROJETO\materio-bootstrap-html-aspnet-core-admin-template-v3.0.0\AspnetCoreStarter\Pages\Admin\Tickets.cshtml.cs'
$content1 = Get-Content $file1 -Raw
$content1 = $content1 -replace '_context\.Tickets\.Add\(NewTicket\);', "NewTicket.Status = `"Pendente`";`r`n            _context.Tickets.Add(NewTicket);"
$content1 | Set-Content $file1 -NoNewline

$file2 = 'c:\Users\Estagio\Documents\GitHub\ASNET-PROJETO\materio-bootstrap-html-aspnet-core-admin-template-v3.0.0\AspnetCoreStarter\Pages\Clients\Directors\Tickets.cshtml.cs'
$content2 = Get-Content $file2 -Raw
$content2 = $content2 -replace 'NewTicket\.Status = `"Pedido`";', 'NewTicket.Status = "Pendente";'
$content2 | Set-Content $file2 -NoNewline
