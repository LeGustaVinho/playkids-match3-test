using System;
using System.Collections.Generic;
using LegendaryTools;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Playkids.Match3
{
    public class Board : IDisposable
    {
        [ShowInInspector] public BoardConfig Config { private set; get; }
        public int MaxShuffles = 5;

        [ShowInInspector] private readonly Tile[][] tiles;

#if UNITY_EDITOR
        [ShowInInspector]
        private PieceType[,] debugPieceBoard
        {
            get
            {
                PieceType[,] pieceBoard = new PieceType[Config.BoardSize.x, Config.BoardSize.y];

                for (int x = 0; x < tiles.Length; x++)
                {
                    for (int y = 0; y < tiles[x].Length; y++)
                    {
                        pieceBoard[x, y] = tiles[x][y].Piece?.Config.Type ?? PieceType.Empty;
                    }
                }

                return pieceBoard;
            }
        }
#endif

        private readonly List<PieceConfig> basicPieces;
        private readonly List<PiecePatternConfig> mergedSortedPiecePatterns = new List<PiecePatternConfig>();

        public Board(BoardConfig config)
        {
            Config = config;
            tiles = config.Board;

            basicPieces = PiecesDB.Instance.FindAll(item => item.Category == PieceCategory.Basic);
            CachePatterns();

            for (int x = 0; x < tiles.Length; x++)
            {
                for (int y = 0; y < tiles[x].Length; y++)
                {
                    tiles[x][y].Initialize(this);

                    if (!tiles[x][y].HasPiece && config.AutoGeneratePieceInEmpty)
                    {
                        Piece newPiece = GenerateRandomBasicPiece();
                        MovePieceTo(newPiece, tiles[x][y]);
                    }
                }
            }
        }

        /// <summary>
        /// Get the tile in position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Tile GetTileAt(Vector2Int position)
        {
            return GetTileAt(position.x, position.y);
        }

        /// <summary>
        /// Get the tile in position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Tile GetTileAt(int x, int y)
        {
            if (x >= 0 && x < tiles.Length)
            {
                if (y >= 0 && y < tiles[x].Length)
                {
                    return tiles[x][y];
                }
            }

            return null;
        }

        //Moves the piece to the tile
        public bool MovePieceTo(Piece piece, Tile tile)
        {
            if (!piece.IsPlaced && !tile.HasPiece && tile.CanPutPiece)
            {
                piece.Tile = tile;
                tile.Piece = piece;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Search for patterns on the board
        /// </summary>
        /// <returns></returns>
        public PatternSearchResult FindPatterns()
        {
            PatternSearchResult searchResult = new PatternSearchResult(); //Stores occurrences of patterns
            HashSet<Tile> allPatternsTiles =
                new HashSet<Tile>(); //Stores all the tiles found in the patterns, this list is used to check the intersection between the patterns.

            //For each pattern (the patterns are ordered in descending order to give more priority to the patterns that have more score
            foreach (PiecePatternConfig matchPattern in mergedSortedPiecePatterns)
            {
                for (int x = 0; x < Config.BoardSize.x; x++)
                {
                    for (int y = 0; y < Config.BoardSize.y; y++)
                    {
                        foreach (PieceConfig basicPiece in basicPieces)
                        {
                            //Search from this tile on the board if there is any pattern that contains the same type of piece
                            List<Tile> tilesFoundInPattern = GetTilesWithSamePiecesFollowingPattern(
                                new Vector2Int(x, y),
                                matchPattern, basicPiece);

                            if (tilesFoundInPattern == null)
                            {
                                continue;
                            }

                            bool tileHasAlreadyBeenFoundInAnyPattern =
                                false; //Flag that prevents a pattern from being found in another pattern

                            //Checks whether the elements in tilesFoundInPattern list intersect with the elements in allPatternsTiles list
                            foreach (Tile tileFoundInPattern in tilesFoundInPattern)
                            {
                                if (allPatternsTiles.Contains(tileFoundInPattern))
                                {
                                    tileHasAlreadyBeenFoundInAnyPattern = true;
                                    break;
                                }
                            }

                            //Only added to the list of found patterns if it does not intersect with any tiles previously found
                            if (!tileHasAlreadyBeenFoundInAnyPattern)
                            {
                                PatternFound newPatternFound = new PatternFound(matchPattern, tilesFoundInPattern);

                                if (matchPattern.IsHint)
                                {
                                    searchResult.Hints.Add(newPatternFound);
                                }
                                else
                                {
                                    searchResult.Matchs.Add(newPatternFound);
                                }

                                allPatternsTiles.AddRange(tilesFoundInPattern);
                            }
                        }
                    }
                }
            }

            return searchResult;
        }

        /// <summary>
        /// Search from tile position on the board if there is any pattern that contains the same type of piece
        /// </summary>
        /// <param name="startBoardIndex"></param>
        /// <param name="allPatternCoords"></param>
        /// <param name="pieceConfig"></param>
        /// <returns>Returns the list of tiles containing this pattern, if not found a pattern returns null</returns>
        public List<Tile> GetTilesWithSamePiecesFollowingPattern(Vector2Int startBoardIndex,
            PiecePatternConfig patternConfig, PieceConfig pieceConfig)
        {
            List<Tile> tiles = new List<Tile>();
            List<Vector2Int> allPatternCoords = patternConfig.PatternCoords;
            foreach (Vector2Int patternCoords in allPatternCoords)
            {
                Tile tile = GetTileAt(startBoardIndex + patternCoords);
                if (tile == null || !tile.HasPiece || !pieceConfig.MatchingWhitelist.Contains(tile.Piece.Config.Type))
                {
                    return null;
                }
                tiles.Add(tile);
            }

            return tiles;
        }

        /// <summary>
        /// Swap the pieces that are on these tiles
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public bool Swap(Tile t1, Tile t2)
        {
            if (!t1.HasPiece || !t2.HasPiece || !t1.CanPutPiece || !t2.CanPutPiece)
            {
                return false;
            }

            Piece piece1 = t1.ReleasePiece();
            Piece piece2 = t2.ReleasePiece();

            bool pieceWasMoved = MovePieceTo(piece1, t2) && MovePieceTo(piece2, t1);

            if (pieceWasMoved)
            {
                //Checks if there was any match
                PatternSearchResult patterns = FindPatterns();
                if (patterns.Matchs.Count > 0)
                {
                    return true;
                }

                //Revert swap because it didn't form any matches
                piece2 = t1.ReleasePiece();
                piece1 = t2.ReleasePiece();
                MovePieceTo(piece1, t1);
                MovePieceTo(piece2, t2);

                return false;
            }

            return false;
        }

        [Button]
        public void DebugSwap(Vector2Int coords1, Vector2Int coords2)
        {
            Tile t1 = GetTileAt(coords1);
            Tile t2 = GetTileAt(coords2);

            Swap(t1, t2);
        }

        /// <summary>
        /// Orchestrates all phases of the board
        /// </summary>
        /// <returns></returns>
        [Button]
        public List<BoardChangeLogEntry> RunBoardPhases()
        {
            List<BoardChangeLogEntry> allBoardChanges = new List<BoardChangeLogEntry>();
            List<BoardChangeLogEntry> patternResolveChanges = PhasePatternSearch(true);

            allBoardChanges.AddRange(patternResolveChanges);

            if (allBoardChanges.Count > 0 && allBoardChanges.Last().Action != BoardChangeAction.PhaseTransition)
            {
                allBoardChanges.Add(new BoardChangeLogEntry(BoardChangeAction.PhaseTransition));
            }

            bool shuffleLimitReached = false;
            
            do
            {
                List<BoardChangeLogEntry> gravityChanges;
                do
                {
                    gravityChanges = PhaseGravity();
                    allBoardChanges.AddRange(gravityChanges);
                    if (allBoardChanges.Count > 0 && allBoardChanges.Last().Action != BoardChangeAction.PhaseTransition)
                    {
                        allBoardChanges.Add(new BoardChangeLogEntry(BoardChangeAction.PhaseTransition));
                    }
                } while (gravityChanges.Count > 0);

                patternResolveChanges = PhasePatternSearch();
                
                if (patternResolveChanges.Exists(item => item.Action == BoardChangeAction.BoardShuffleLimitReached))
                {
                    if (shuffleLimitReached)
                    {
                        allBoardChanges.Add(new BoardChangeLogEntry(BoardChangeAction.BoardShuffleLimitReached));
                        return allBoardChanges;
                    }
                    
                    shuffleLimitReached = true;
                }
                else
                {
                    shuffleLimitReached = false;
                    allBoardChanges.AddRange(patternResolveChanges);
                }

            } while (patternResolveChanges.Count > 0);

            return allBoardChanges;
        }

        /// <summary>
        /// Search for patterns on the board (match patterns and hint patterns).
        /// </summary>
        /// <param name="preventShuffle"></param>
        /// <returns></returns>
        [Button]
        public List<BoardChangeLogEntry> PhasePatternSearch(bool preventShuffle = false)
        {
            List<BoardChangeLogEntry> changes = new List<BoardChangeLogEntry>();
            PatternSearchResult patternSearchResult = FindPatterns();

            if (patternSearchResult.TotalCount > 0)
            {
                changes.AddRange(PhasePatternResolve(patternSearchResult));
            }
            else
            {
                if (preventShuffle)
                {
                    return changes;
                }
                
                int shuffleCount = 0;
                do
                {
                    changes.Clear();
                    if (shuffleCount > MaxShuffles) //Prevents infinite shuffles
                    {
                        changes.Add(new BoardChangeLogEntry(BoardChangeAction.BoardShuffleLimitReached));
                        return changes;
                    }

                    changes.Add(new BoardChangeLogEntry(BoardChangeAction.BoardShuffle));
                    changes.AddRange(ShufflePieces());
                    changes.Add(new BoardChangeLogEntry(BoardChangeAction.PhaseTransition));
                    patternSearchResult = FindPatterns();
                    shuffleCount++;

                    //If there is no pattern in this shuffle, revert to the previous state
                    if (patternSearchResult.TotalCount == 0)
                    {
                        RevertShuffle(changes);
                    }
                    
                } while (patternSearchResult.TotalCount == 0);
            }

            return changes;
        }

        /// <summary>
        /// Solve the matchs of the pieces
        /// </summary>
        /// <param name="patternSearchResult"></param>
        /// <returns></returns>
        public List<BoardChangeLogEntry> PhasePatternResolve(PatternSearchResult patternSearchResult)
        {
            List<BoardChangeLogEntry> changes = new List<BoardChangeLogEntry>();
            foreach (PatternFound match in patternSearchResult.Matchs)
            {
                changes.Add(new BoardChangeLogEntry(match));
                foreach (Tile tile in match.Tiles)
                {
                    Piece destroyedPiece = tile.DestroyPiece();
                    if (destroyedPiece != null)
                    {
                        changes.Add(new BoardChangeLogEntry(tile, destroyedPiece, BoardChangeAction.PieceDestroy));
                    }
                }
            }
            return changes;
        }

        /// <summary>
        /// Performs gravity on the board pieces
        /// </summary>
        /// <returns></returns>
        [Button]
        public List<BoardChangeLogEntry> PhaseGravity()
        {
            List<BoardChangeLogEntry> changes = new List<BoardChangeLogEntry>();
            List<Tile> tilesOfPiecesWillFall = new List<Tile>();
            List<Tile> tilesInGravityFlow = new List<Tile>();
            Tile[] tilesWithPieceGenerator = FindTilesWithPieceGenerator();

            //Search for tiles that contain a piece that may fall with gravity
            foreach (Tile[] columns in tiles)
            {
                Tile[] tiles = Array.FindAll(columns,
                    item => item.HasPiece && item.GravitationalChild != null && !item.GravitationalChild.HasPiece &&
                            item.GravitationalChild.CanPutPiece);
            
                tilesOfPiecesWillFall.AddRange(tiles);
            }

            //Group the pieces that will fall into gravity together
            foreach (Tile tileOfPieceWillFall in tilesOfPiecesWillFall)
            {
                tilesInGravityFlow.Add(tileOfPieceWillFall); //this piece will surely fall
                
                Tile parentGravitationalTile = tileOfPieceWillFall.GravitationalParent;
                while (parentGravitationalTile != null)
                {
                    if (parentGravitationalTile.HasPiece)
                    {
                        tilesInGravityFlow.Add(parentGravitationalTile);
                        parentGravitationalTile = parentGravitationalTile.GravitationalParent;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            
            //Move pieces in gravity flow
            foreach (Tile tileInGravityFlow in tilesInGravityFlow)
            {
                if (tileInGravityFlow.HasPiece)
                {
                    Piece releasedPiece = tileInGravityFlow.ReleasePiece();
                    if (MovePieceTo(releasedPiece, tileInGravityFlow.GravitationalChild))
                    {
                        changes.Add(new BoardChangeLogEntry(tileInGravityFlow,
                            tileInGravityFlow.GravitationalChild,
                            releasedPiece));
                    }
                }
            }
            
            //Try create a piece on tile with piece generator
            foreach (Tile tileWithPieceGenerator in tilesWithPieceGenerator)
            {
                if (!tileWithPieceGenerator.HasPiece && tileWithPieceGenerator.CanPutPiece)
                {
                    if (tileWithPieceGenerator.TryGeneratePiece())
                    {
                        changes.Add(new BoardChangeLogEntry(tileWithPieceGenerator, tileWithPieceGenerator.Piece,
                            BoardChangeAction.PieceCreation));
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// Find tiles that have a tile generator
        /// </summary>
        /// <returns></returns>
        private Tile[] FindTilesWithPieceGenerator()
        {
            List<Tile> tilesWithPieceGenerator = new List<Tile>();
            foreach (Tile[] columns in tiles)
            {
                tilesWithPieceGenerator.AddRange(Array.FindAll(columns, item => item.PieceGenerator != null));
            }

            return tilesWithPieceGenerator.ToArray();
        }

        /// <summary>
        /// Shuffle the board pieces randomly
        /// </summary>
        /// <returns></returns>
        public List<BoardChangeLogEntry> ShufflePieces()
        {
            List<BoardChangeLogEntry> changes = new List<BoardChangeLogEntry>();
            List<Tile> availableTiles = GetAllTilesWithPieces(); //stores tiles that have not yet been shuffled

            //the algorithm exchanges the pieces in pairs, so if it is odd it is not possible to continue
            while (availableTiles.Count > 1)
            {
                if (availableTiles.Count < 2) //
                {
                    break;
                }

                //get first piece randomly
                int rndIndex1 = Random.Range(0, availableTiles.Count);
                Tile tile1 = availableTiles[rndIndex1];
                Piece piece1 = tile1.ReleasePiece();
                availableTiles.RemoveAt(rndIndex1);
                
                //get second piece randomly
                int rndIndex2 = Random.Range(0, availableTiles.Count);
                Tile tile2 = availableTiles[rndIndex2];
                Piece piece2 = tile2.ReleasePiece();
                availableTiles.RemoveAt(rndIndex2);

                if (MovePieceTo(piece1, tile2) && MovePieceTo(piece2, tile1)) //swap positions
                {
                    changes.Add(new BoardChangeLogEntry(tile1, tile2, piece1, piece2));
                }
            }

            return changes;
        }

        /// <summary>
        /// Get all tiles that contain a piece
        /// </summary>
        /// <returns></returns>
        public List<Tile> GetAllTilesWithPieces()
        {
            List<Tile> allTiles = new List<Tile>();

            for (int x = 0; x < tiles.Length; x++)
            {
                for (int y = 0; y < tiles[x].Length; y++)
                {
                    if (tiles[x][y].HasPiece)
                    {
                        allTiles.Add(tiles[x][y]);
                    }
                }
            }
            
            return allTiles;
        }

        /// <summary>
        /// Reverses the movements made by the shuffle
        /// </summary>
        /// <param name="shuffleChanges"></param>
        private void RevertShuffle(List<BoardChangeLogEntry> shuffleChanges)
        {
            foreach (BoardChangeLogEntry shuffleChange in shuffleChanges)
            {
                if (shuffleChange.Action == BoardChangeAction.PieceMoveShuffle)
                {
                    Piece fromPiece = shuffleChange.FromTile.ReleasePiece();
                    Piece toPiece = shuffleChange.ToTile.ReleasePiece();

                    MovePieceTo(fromPiece, shuffleChange.ToTile);
                    MovePieceTo(toPiece, shuffleChange.FromTile);
                }
            }
        }
        
        /// <summary>
        /// Get all patterns and stores sorted by score
        /// </summary>
        private void CachePatterns()
        {
            List<PiecePatternConfig> sortedMatchPatterns =
                new List<PiecePatternConfig>(PiecePatternDB.Instance.MatchPatterns);
            sortedMatchPatterns.Sort((a, b) => b.Score.CompareTo(a.Score)); //Sort desc

            List<PiecePatternConfig> sortedHintPatterns =
                new List<PiecePatternConfig>(PiecePatternDB.Instance.HintPatterns);
            sortedHintPatterns.Sort((a, b) => b.Score.CompareTo(a.Score)); //Sort desc

            mergedSortedPiecePatterns.AddRange(sortedMatchPatterns); //Add matches first to take priority
            mergedSortedPiecePatterns.AddRange(sortedHintPatterns);
        }

        /// <summary>
        /// Generate a basic piece
        /// </summary>
        /// <returns>generated piece</returns>
        private Piece GenerateRandomBasicPiece()
        {
            return new Piece(basicPieces[Random.Range(0, basicPieces.Count)]);
        }

        public void Dispose()
        {
            
        }
    }
}