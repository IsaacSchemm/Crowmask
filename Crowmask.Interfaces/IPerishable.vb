''' <summary>
''' An item that is only considered "fresh" for a certain amount of time, based on its initial post date and last refresh date.
''' </summary>
Public Interface IPerishable
    ''' <summary>
    ''' The date and time at which the item was originally posted to Weasyl.
    ''' </summary>
    ReadOnly Property PostedAt As DateTimeOffset

    ''' <summary>
    ''' The last time Crowmask attempted to refresh this item.
    ''' </summary>
    ReadOnly Property CacheRefreshAttemptedAt As DateTimeOffset

    ''' <summary>
    ''' The last time Crowmask successfully refreshed this item.
    ''' </summary>
    ReadOnly Property CacheRefreshSucceededAt As DateTimeOffset
End Interface
