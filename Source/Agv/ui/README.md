# WebUI

The UI is written in TypeScript using [vuejs 3](https://vuejs.org/) and [BalmUI](https://material.balmjs.com/).

## Development

The UI development requires npm v18 to be installed

- Install npm as described by [Microsoft](https://docs.microsoft.com/en-us/windows/dev-environment/javascript/nodejs-on-windows)
  - Download and install [nvm](https://github.com/coreybutler/nvm-windows)
    - When development is done using a non-admin user take care to use ```"C:\Program Files\nvm"``` and ```"C:\Program Files\nodejs"``` for installation path instead of the default ```C:\Users\some_admin_user\AppData\Roaming\...```.
    - When ```npm``` is not found by the development user add these two pathes to the PATH environment variable.
  - Run: nvm install latest
  - Run as Admin: nvm use <version>

### Install all requirements:

```
npm install
```

### Run in developer mode.

```
npm run dev
```

It is expected that TransportOrder is running, listening on `127.0.0.1:5000` for backend requests.

### Build

```
npm run build
```

### Additional Commands

Code Style cleanup

```
npm run format
```

Run linter

```
npm run lint
```

## Generate Api

Download the current swagger.json from the [swagger-ui](http://127.0.0.1:5000/swagger/v1/swagger.json)
The api client in `src/services/api` is generated using [swagger-typescript-api](https://github.com/acacode/swagger-typescript-api)

### Install

```
npm install swagger-typescript-api
```

### Generate

```
npx swagger-typescript-api -p ./src/services/api/swagger.json -o ./src/services/api --modular --type-prefix=I
```

All data-type definitions are then generated with an IPrefix, to overcome name conflicts.
