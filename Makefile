.PHONY: dev dev-api dev-web dev-marketing build build-web build-marketing

dev-api:
	dotnet watch --project src/MyMarina.Api

dev-web:
	cd src/MyMarina.Web && npm run dev

dev-marketing:
	cd src/MyMarina.Marketing && npm run dev

build-web:
	cd src/MyMarina.Web && npm run build

build-marketing:
	cd src/MyMarina.Marketing && npm run build
