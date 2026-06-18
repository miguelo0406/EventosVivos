# Imagen custom de Keycloak 26: hornea el realm dentro de la imagen para que
# --import-realm lo encuentre sin bind-mount (que no existe en la nube).
FROM quay.io/keycloak/keycloak:26.0

# Keycloak lee automáticamente los .json de /opt/keycloak/data/import al arrancar con --import-realm.
COPY keycloak/import/eventosvivos-realm.json /opt/keycloak/data/import/eventosvivos-realm.json

# 'start' = modo producción. El import es idempotente: solo crea el realm la primera vez;
# como Keycloak persiste en la base 'keycloak' de Postgres, en arranques siguientes lo omite.
ENTRYPOINT ["/opt/keycloak/bin/kc.sh", "start", "--import-realm"]
