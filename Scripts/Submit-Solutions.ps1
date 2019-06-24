$root = "C:\Users\cashto\Documents\GitHub\icfp2019"

Compress-Archive -Force -Path "$root\work\best_solutions\*" -DestinationPath "$root\work\solutions.zip"

& curl.exe -F "private_id=237f94301c5ccd4e8d81a594" -F "file=@solutions.zip" https://monadic-lab.org/submit