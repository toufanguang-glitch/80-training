$in = [Console]::In.ReadToEnd()
if ($in -match 'DROP TABLE|TRUNCATE|DELETE') {
    [Console]::Error.WriteLine('Action denied')
    exit 2
}

exit 0
