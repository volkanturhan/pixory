using System.Collections.ObjectModel;
using pixory.Models;

namespace pixory.Services;

/// <summary>
/// Keeps the most recently picked colours in memory — the palette. Ordering is
/// "pinned first, then most recent": pinned colours sit at the top and are kept
/// forever, while the unpinned colours below them are newest-first and capped to
/// <see cref="Capacity"/> (older ones drop off).
///
/// Picking a colour that is already in the palette moves it to the top of its
/// section instead of creating a duplicate.
///
/// <see cref="Items"/> is observable so the UI can bind to it directly, and
/// <see cref="Changed"/> fires after any mutation so the persistence layer can
/// react. All mutation happens on the UI thread.
/// </summary>
public sealed class ColorHistory
{
    private readonly ObservableCollection<PickedColor> _items = new();

    public ColorHistory(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity),
                "Palette capacity must be at least 1.");

        Capacity = capacity;
        Items = new ReadOnlyObservableCollection<PickedColor>(_items);
    }

    /// <summary>Number of unpinned colours kept by default.</summary>
    public const int DefaultCapacity = 50;

    /// <summary>Maximum number of unpinned colours retained before the oldest drops off.</summary>
    public int Capacity { get; }

    /// <summary>The palette, pinned-first then newest-first, exposed read-only for binding.</summary>
    public ReadOnlyObservableCollection<PickedColor> Items { get; }

    /// <summary>Raised after any change to the palette (add, pin, remove, clear).</summary>
    public event Action? Changed;

    /// <summary>Replaces the current palette with the given colours (used on load).</summary>
    public void Initialize(IEnumerable<PickedColor> colors)
    {
        _items.Clear();
        foreach (var color in colors)
            _items.Add(color);

        PinnedFirst();
        TrimUnpinned();
        Changed?.Invoke();
    }

    /// <summary>
    /// Records a freshly picked colour. If the same RGB value is already in the
    /// palette it is promoted to the top of its section rather than duplicated.
    /// Returns the entry now sitting at the top (new or promoted).
    /// </summary>
    public PickedColor Add(byte r, byte g, byte b)
    {
        var existingIndex = IndexOf(r, g, b);
        if (existingIndex >= 0)
        {
            var existing = _items[existingIndex];
            var promoteTo = existing.IsPinned ? 0 : PinnedCount;
            if (existingIndex != promoteTo)
                _items.Move(existingIndex, promoteTo);
            Changed?.Invoke();
            return existing;
        }

        // New colours go to the top of the unpinned section.
        var added = new PickedColor(r, g, b, DateTime.Now);
        _items.Insert(PinnedCount, added);
        TrimUnpinned();
        Changed?.Invoke();
        return added;
    }

    /// <summary>Pins or unpins a colour and moves it to the top of its new section.</summary>
    public void TogglePin(PickedColor color)
    {
        var index = _items.IndexOf(color);
        if (index < 0)
            return;

        color.IsPinned = !color.IsPinned;

        var target = color.IsPinned ? 0 : PinnedCount;
        if (index != target)
            _items.Move(index, target);

        TrimUnpinned();
        Changed?.Invoke();
    }

    /// <summary>Removes a single colour from the palette.</summary>
    public void Remove(PickedColor color)
    {
        if (_items.Remove(color))
            Changed?.Invoke();
    }

    /// <summary>Clears the unpinned colours, keeping pinned ones.</summary>
    public void ClearUnpinned()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (!_items[i].IsPinned)
                _items.RemoveAt(i);
        }

        Changed?.Invoke();
    }

    private int PinnedCount
    {
        get
        {
            var count = 0;
            foreach (var item in _items)
            {
                if (item.IsPinned)
                    count++;
            }

            return count;
        }
    }

    private int IndexOf(byte r, byte g, byte b)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].R == r && _items[i].G == g && _items[i].B == b)
                return i;
        }

        return -1;
    }

    // Stable partition so every pinned colour precedes every unpinned one while
    // each group keeps its existing relative order.
    private void PinnedFirst()
    {
        var insertAt = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].IsPinned)
            {
                if (i != insertAt)
                    _items.Move(i, insertAt);
                insertAt++;
            }
        }
    }

    // Keep only the newest Capacity unpinned colours; drop older unpinned ones.
    private void TrimUnpinned()
    {
        var unpinnedSeen = 0;
        var i = 0;
        while (i < _items.Count)
        {
            if (!_items[i].IsPinned && ++unpinnedSeen > Capacity)
            {
                _items.RemoveAt(i);
                continue;
            }

            i++;
        }
    }
}
