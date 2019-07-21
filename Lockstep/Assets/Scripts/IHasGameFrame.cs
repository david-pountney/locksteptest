using System;

public interface IHasGameFrame
{
	void GameFrameTurn(int gameFramesPerSecond);
	
	bool Finished { get; }

    void GameFrameTurn(object gameFramesPerSecond);
}
