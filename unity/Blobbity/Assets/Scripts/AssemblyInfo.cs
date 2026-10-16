using System.Runtime.CompilerServices;

// The play-mode tests reach into the view's internals (Ui and the painters) to look at what was drawn.
[assembly: InternalsVisibleTo("Blobbity.Player.Tests")]
