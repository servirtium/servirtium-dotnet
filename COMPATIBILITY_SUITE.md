# Servirtium Compatibility Suite Runner

The compatibility suite allows us to check this .NET version of Servirtium against other language implementations. Regular users of Servirtim are unlikely to deploy Servirtium in this configuration. People contributing to Servirtium development will be interested in this.

You'll need to have Docker, Python3 installed in addition to the .NET core SDK 3.1

## Running this from a cloned/checked out directory

```
git clone git@github.com:servirtium/servirtium-dotnet.git
cd servirtium-dotnet
build.bat (or ./build.sh)
docker-compose build
python3 path/to/compatibility-suite-runner/compatibility-suite.py record -p 61417
```

That records 16 interactions with out test suite - you'll need to be online

```
python3 path/to/compatibility-suite-runner/compatibility-suite.py playback -p 61417
```

That replays the 16 records interactions with out test suite - you can be offline and this will still work

## Running this GitHub without cloning

Note 'record' and 'playback' above.

### Mac & Linux

As above but thve Python3 line should be

```
curl -s https://raw.githubusercontent.com/servirtium/compatibility-suite-runner/main/compatibility-suite.py \
  | python3 /dev/stdin record -p 61417
```

### Windows

TODO

## Running this without installing .NET core

TODO - Docker way - note that it's slower.