''' <summary>
''' Provides the hostname of the Azure Key Vault instance used by Crowmask.
''' </summary>
Public Interface IKeyVaultHost
    ''' <summary>
    ''' The host / domain name of the vault.
    ''' </summary>
    ReadOnly Property Hostname As String
End Interface
